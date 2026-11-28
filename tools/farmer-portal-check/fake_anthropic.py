"""
A LOCAL stand-in for the Anthropic Messages API, for farmer portal Task 5 and Task 6 tests only. It never calls Anthropic.

    python tools/farmer-portal-check/fake_anthropic.py [--port 5999] [--delay 0] [--record <file>]

Point a LOCAL API at it (development only):

    $env:Assistant__AnthropicApiKey = "local-stub"
    $env:Assistant__Endpoint = "http://127.0.0.1:5999/v1/messages"

Each POST is answered with a fixed Messages API reply (a thinking block, a text block ending in a SUGGESTIONS line,
stop_reason end_turn) after --delay seconds, so the chat's typing indicator can be seen. The last request body is
written to --record (the smoke test reads it to check the system prompt and the farmer context). A question that
contains "[refuse]" gets stop_reason "refusal"; "[fail]" gets HTTP 500 (both must fall back to the basic answer).
Task 6: a request with an image block (the leaf check) gets a vision JSON answer that follows the app's pattern hint;
POST /__mode {"vision": "auto|malformed|fail|refuse|notleaf"} switches it (see VISION below).
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


# ---- Task 6: leaf-photo check (vision) ----
# A request whose user message has an image block gets a vision answer in the leaf check's JSON shape. "auto" follows
# the app's own pattern hint in the text block (so each test leaf gets a fitting answer); POST /__mode {"vision": ...}
# switches to "malformed" (truncated JSON), "fail" (HTTP 500), "refuse" (stop_reason refusal), "notleaf", or back.
STATE = {"vision": "auto"}

VISION = {
    "none": {"condition": "healthy", "nutrients": [], "symptoms": ["Even dark green colour", "No spots or dry edges"],
             "leafAge": "unknown", "confidence": 0.82,
             "advice": ["Your leaf looks healthy. Keep following your fertilizer plan.",
                        "Check a few more leaves from other parts of the field once a week."],
             "needsSoilTest": False, "notALeaf": False},
    "uniform": {"condition": "nutrient_deficiency", "nutrients": [{"code": "N", "likelihood": 0.78}, {"code": "S", "likelihood": 0.22}],
                "symptoms": ["Pale yellow colour over the whole leaf", "Veins are pale too", "No spots or dry edges"],
                "leafAge": "older", "confidence": 0.74,
                "advice": ["Give the next urea top-dressing now, on moist soil.",
                           "Look at the lower leaves: if they are the yellowest, nitrogen is the likely cause.",
                           "Do not give more than your fertilizer advice says."],
                "needsSoilTest": True, "notALeaf": False},
    "interveinal": {"condition": "nutrient_deficiency", "nutrients": [{"code": "Fe", "likelihood": 0.62}, {"code": "Zn", "likelihood": 0.41}, {"code": "Mn", "likelihood": 0.2}],
                    "symptoms": ["Yellow tissue between green veins", "The veins stay dark green", "No brown spots"],
                    "leafAge": "younger", "confidence": 0.68,
                    "advice": ["Check whether the young top leaves are the most yellow.",
                               "Spray in the evening, when the sun is low.",
                               "Ask your SPIC officer about a soil test for iron and zinc."],
                    "needsSoilTest": True, "notALeaf": False},
    "margin": {"condition": "nutrient_deficiency", "nutrients": [{"code": "K", "likelihood": 0.72}, {"code": "Mg", "likelihood": 0.18}],
               "symptoms": ["Brown, dry leaf edges", "A yellow band inside the brown edge", "The centre of the leaf is green"],
               "leafAge": "older", "confidence": 0.7,
               "advice": ["Look at the older, lower leaves: potassium shortage starts there.",
                          "Keep the soil moist; dry soil makes it worse.",
                          "Include potash in the next fertilizer dose."],
               "needsSoilTest": True, "notALeaf": False},
    "notleaf": {"condition": "unclear", "nutrients": [], "symptoms": ["A printed page on a table, no leaf"],
                "leafAge": "unknown", "confidence": 0.9,
                "advice": ["Take a photo of one leaf, close up, in daylight."], "needsSoilTest": False, "notALeaf": True},
}


def vision_answer(text):
    mode = STATE["vision"]
    if mode == "notleaf":
        return json.dumps(VISION["notleaf"])
    if mode == "malformed":
        return json.dumps(VISION["uniform"])[:70]
    hint = "none"
    for key in ("interveinal", "uniform", "margin", "patchy", "spots", "none"):
        if f"pattern hint: {key}" in text:
            hint = key
            break
    answer = dict(VISION.get(hint) or VISION["uniform"])
    if hint == "interveinal" and "older" in text:
        answer = dict(answer, nutrients=[{"code": "Mg", "likelihood": 0.66}, {"code": "K", "likelihood": 0.2}], leafAge="older")
    return json.dumps(answer)


def make_handler(args):
    class Handler(BaseHTTPRequestHandler):
        def log_message(self, fmt, *a):  # quiet
            pass

        def do_POST(self):
            length = int(self.headers.get("Content-Length") or 0)
            raw = self.rfile.read(length).decode("utf-8")
            if self.path.startswith("/__mode"):
                STATE.update(json.loads(raw or "{}"))
                self.send_response(204)
                self.end_headers()
                return
            if args.record:
                with open(args.record, "w", encoding="utf-8") as f:
                    f.write(raw)
            body = json.loads(raw or "{}")
            last = ""
            image = False
            try:
                blocks = body["messages"][-1]["content"]
                image = any(b.get("type") == "image" for b in blocks)
                last = next((b["text"] for b in blocks if b.get("type") == "text"), "")
            except (KeyError, IndexError, TypeError, AttributeError):
                pass
            if args.delay > 0:
                time.sleep(args.delay)
            if image:
                mode = STATE["vision"]
                if mode == "fail":
                    last = "[fail]"
                elif mode == "refuse":
                    last = "[refuse]"
                else:
                    reply = {"id": "msg_stub", "type": "message", "role": "assistant", "model": body.get("model"),
                             "content": [{"type": "thinking", "thinking": "", "signature": "stub"}, {"type": "text", "text": vision_answer(last)}],
                             "stop_reason": "end_turn", "usage": {"input_tokens": 1, "output_tokens": 1}}
                    data = json.dumps(reply).encode("utf-8")
                    self.send_response(200)
                    self.send_header("Content-Type", "application/json")
                    self.send_header("Content-Length", str(len(data)))
                    self.end_headers()
                    self.wfile.write(data)
                    return
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
