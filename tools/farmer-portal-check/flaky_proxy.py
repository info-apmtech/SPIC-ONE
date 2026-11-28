"""
A LOCAL test proxy between the local web host and the local API, for recheck residual N3 (transient refresh failures).

    python tools/farmer-portal-check/flaky_proxy.py [--listen 5121] [--target http://localhost:5120]

Run the web host with --ApiBaseUrl=http://localhost:5121/ so its API calls pass through here. Every request is forwarded
unchanged, except while a failure mode is switched on (only from this machine):

    GET /__proxy/mode?refresh=ok|503|drop      api/Authentication/refresh answers normally, 503, or the connection is closed
    GET /__proxy/mode?all=ok|503|drop          the same for every request

Local only: it listens on 127.0.0.1 and refuses a non-local target. Nothing is logged but the method, the path without the
query string and the status.
"""
import argparse
import http.client
import sys
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from urllib.parse import parse_qs, urlparse

MODE = {"refresh": "ok", "all": "ok"}
HOP = {"connection", "keep-alive", "proxy-authenticate", "proxy-authorization", "te", "trailers", "transfer-encoding", "upgrade", "host"}


class Proxy(BaseHTTPRequestHandler):
    protocol_version = "HTTP/1.1"
    target = None

    def log_message(self, fmt, *args):
        pass

    def _fail(self, mode):
        if mode == "drop":
            self.close_connection = True
            try:
                self.connection.shutdown(2)
            except OSError:
                pass
            return
        body = b'{"message":"proxy test 503"}'
        self.send_response(503)
        self.send_header("Content-Type", "application/json")
        self.send_header("Content-Length", str(len(body)))
        self.end_headers()
        self.wfile.write(body)

    def _handle(self):
        path = urlparse(self.path).path
        if path == "/__proxy/mode":
            for k, v in parse_qs(urlparse(self.path).query).items():
                if k in MODE and v and v[0] in ("ok", "503", "drop"):
                    MODE[k] = v[0]
            body = str(MODE).encode()
            self.send_response(200)
            self.send_header("Content-Length", str(len(body)))
            self.end_headers()
            self.wfile.write(body)
            return
        if "chunked" in (self.headers.get("Transfer-Encoding") or "").lower():
            # .NET's JsonContent posts chunked: read the chunks and forward one body with a length.
            parts = []
            while True:
                size = int(self.rfile.readline().strip().split(b";")[0] or b"0", 16)
                if size == 0:
                    while self.rfile.readline() not in (b"\r\n", b"\n", b""):
                        pass
                    break
                parts.append(self.rfile.read(size))
                self.rfile.readline()
            data = b"".join(parts)
        else:
            length = int(self.headers.get("Content-Length") or 0)
            data = self.rfile.read(length) if length else None
        if MODE["all"] != "ok":
            return self._fail(MODE["all"])
        if MODE["refresh"] != "ok" and path.lower().endswith("/api/authentication/refresh"):
            return self._fail(MODE["refresh"])
        t = urlparse(self.target)
        conn = http.client.HTTPConnection(t.hostname, t.port or 80, timeout=60)
        headers = {k: v for k, v in self.headers.items() if k.lower() not in HOP}
        conn.request(self.command, self.path, body=data, headers=headers)
        resp = conn.getresponse()
        payload = resp.read()
        self.send_response(resp.status)
        for k, v in resp.getheaders():
            if k.lower() not in HOP and k.lower() != "content-length":
                self.send_header(k, v)
        self.send_header("Content-Length", str(len(payload)))
        self.end_headers()
        self.wfile.write(payload)
        conn.close()

    do_GET = do_POST = do_PUT = do_DELETE = do_PATCH = _handle


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--listen", type=int, default=5121)
    ap.add_argument("--target", default="http://localhost:5120")
    args = ap.parse_args()
    if (urlparse(args.target).hostname or "") not in ("localhost", "127.0.0.1"):
        sys.exit("Refusing a non-local target: this proxy is for the LOCAL API only.")
    Proxy.target = args.target
    print(f"flaky proxy 127.0.0.1:{args.listen} -> {args.target}", flush=True)
    ThreadingHTTPServer(("127.0.0.1", args.listen), Proxy).serve_forever()


if __name__ == "__main__":
    main()
