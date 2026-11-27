"""
A LOCAL stand-in for the Anthropic Messages API, for farmer portal Task 5 tests only. It never calls Anthropic.

    python tools/farmer-portal-check/fake_anthropic.py [--port 5999] [--delay 0] [--record <file>]

Point a LOCAL API at it (development only):

    $env:Assistant__AnthropicApiKey = "local-stub"
    $env:Assistant__Endpoint = "http://127.0.0.1:5999/v1/messages"

Each POST is answered with a fixed Messages API reply (a thinking block, a text block ending in a SUGGESTIONS line,
stop_reason end_turn) after --delay seconds, so the chat's typing indicator can be seen. The last request body is
written to --record (the smoke test reads it to check the system prompt and the farmer context). A question that
contains "[refuse]" gets stop_reason "refusal"; "[fail]" gets HTTP 500 (both must fall back to the basic answer).
Binds to 127.0.0.1 only.
"""
import argparse
import json
import time
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer

ANSWER = (
    "Yellow leaves in young paddy usually mean the crop is short of nitrogen.\n\n"
    "1. Give the next urea split now, on moist soil, not before heavy rain.\n"
    "2. Use the dose from your fertilizer advice in the app; do not add more.\n"
    "3. Keep 2-3 cm of water in the field for a week after.\n"
    "4. If the yellowing spreads in patches or the leaves show spots, ask your SPIC officer (MDO) through My SPIC team for a field visit.\n\n"
    "SPIC Gromor Urea (46-0-0) supplies the nitrogen.\n"
    "SUGGESTIONS: How much urea per acre for paddy? | When should I give the last urea split? | Can I mix urea with potash?"
)


def make_handler(args):
    class Handler(BaseHTTPRequestHandler):
        def log_message(self, fmt, *a):  # quiet
            pass

        def do_POST(self):
            length = int(self.headers.get("Content-Length") or 0)
            raw = self.rfile.read(length).decode("utf-8")
            if args.record:
                with open(args.record, "w", encoding="utf-8") as f:
                    f.write(raw)
            body = json.loads(raw or "{}")
            last = ""
            try:
                last = body["messages"][-1]["content"][0]["text"]
            except (KeyError, IndexError, TypeError):
                pass
            if args.delay > 0:
                time.sleep(args.delay)
            if "[fail]" in last:
                self.send_response(500)
                self.send_header("Content-Type", "application/json")
                self.end_headers()
                self.wfile.write(b'{"type":"error","error":{"type":"api_error","message":"stub failure"}}')
                return
            stop = "refusal" if "[refuse]" in last else "end_turn"
            content = [] if stop == "refusal" else [{"type": "thinking", "thinking": "", "signature": "stub"}, {"type": "text", "text": ANSWER}]
            reply = {"id": "msg_stub", "type": "message", "role": "assistant", "model": body.get("model"),
                     "content": content, "stop_reason": stop, "usage": {"input_tokens": 1, "output_tokens": 1}}
            data = json.dumps(reply).encode("utf-8")
            self.send_response(200)
            self.send_header("Content-Type", "application/json")
            self.send_header("Content-Length", str(len(data)))
            self.end_headers()
            self.wfile.write(data)

    return Handler


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--port", type=int, default=5999)
    ap.add_argument("--delay", type=float, default=0.0)
    ap.add_argument("--record", default="")
    args = ap.parse_args()
    server = ThreadingHTTPServer(("127.0.0.1", args.port), make_handler(args))
    print(f"fake Anthropic on http://127.0.0.1:{args.port}/v1/messages (delay {args.delay}s)", flush=True)
    server.serve_forever()


if __name__ == "__main__":
    main()
