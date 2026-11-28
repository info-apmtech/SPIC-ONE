"""
Farmer portal Task 6 smoke test: the leaf-photo check (api/Farmer/leaf-check), against a LOCAL API and the LOCAL
spicone_dev database only. Test photos come from make_leaves.py (drawn, never downloaded):

    python tools/farmer-portal-check/make_leaves.py --out <scratch>/leaves

Basic mode (no Anthropic key; the "basic colour check"):

    $env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5432;Database=spicone_dev;Username=postgres;Password=1234"
    dotnet run --project SpicAPI --no-launch-profile --urls http://localhost:5120     (ASPNETCORE_ENVIRONMENT=Development)
    python tools/farmer-portal-check/smoke_task6.py --leaves <scratch>/leaves

Stub mode (Claude vision against tools/farmer-portal-check/fake_anthropic.py, never the real API):

    python tools/farmer-portal-check/fake_anthropic.py --record <scratch>/last.json
    $env:Assistant__AnthropicApiKey = "local-stub"; $env:Assistant__Endpoint = "http://127.0.0.1:5999/v1/messages"
    $env:FarmerPortal__AiKindDailyLimit__LeafCheck = "8"      (so the budget mode below can be reached)
    dotnet run --project SpicAPI --no-launch-profile --urls http://localhost:5120
    python tools/farmer-portal-check/smoke_task6.py --mode stub --leaves <scratch>/leaves --record <scratch>/last.json

Kill switch: the stub API plus $env:FarmerPortal__AssistantEnabled = "false":  --mode kill

Each run uses up leaf checks of the test farmers for today's IST day; cleanup_task6_local.sql removes the local test
farmers' checks and LeafCheck AI usage rows (delete SpicAPI/PrivateUploads/FarmerLeaf by hand for the photo files).
"""
import argparse
import base64
import io
import json
import os
import sys
import threading
import urllib.error
import urllib.request
import uuid

sys.path.insert(0, os.path.dirname(__file__))
from smoke_task3 import RUN_IP, ROOT, call, check, failures, farmer_token, staff_token, qa_password  # noqa: E402

# Fix round 1: leaf photos live in the private root (SpicAPI/PrivateUploads), outside the shared Uploads tree.
UPLOADS = os.path.join(ROOT, "SpicAPI", "PrivateUploads", "FarmerLeaf")
SHARED_UPLOADS = os.path.join(ROOT, "SpicAPI", "Uploads", "FarmerLeaf")


def post_photo(base, token, data, crop="Paddy", leaf_age=None, content_type="image/jpeg", filename="leaf.jpg"):
    boundary = "----leaf" + uuid.uuid4().hex
    parts = []

    def field(name, value):
        parts.append(f'--{boundary}\r\nContent-Disposition: form-data; name="{name}"\r\n\r\n{value}\r\n'.encode())

    if crop is not None:
        field("crop", crop)
    if leaf_age is not None:
        field("leafAge", leaf_age)
    if data is not None:
        parts.append((f'--{boundary}\r\nContent-Disposition: form-data; name="photo"; filename="{filename}"\r\n'
                      f"Content-Type: {content_type}\r\n\r\n").encode() + data + b"\r\n")
    parts.append(f"--{boundary}--\r\n".encode())
    req = urllib.request.Request(base.rstrip("/") + "/api/Farmer/leaf-check", data=b"".join(parts), method="POST")
    req.add_header("Content-Type", f"multipart/form-data; boundary={boundary}")
    req.add_header("X-Forwarded-For", RUN_IP)
    if token:
        req.add_header("Authorization", token if token.startswith("Bearer ") else "Bearer " + token)
    try:
        with urllib.request.urlopen(req, timeout=120) as resp:
            return resp.status, json.loads(resp.read().decode() or "null"), dict(resp.headers)
    except urllib.error.HTTPError as e:
        raw = e.read().decode()
        try:
            return e.code, json.loads(raw), dict(e.headers)
        except ValueError:
            return e.code, raw, dict(e.headers)


def get_bytes(base, path, token):
    req = urllib.request.Request(base.rstrip("/") + "/" + path, method="GET")
    req.add_header("X-Forwarded-For", RUN_IP)
    if token:
        req.add_header("Authorization", token if token.startswith("Bearer ") else "Bearer " + token)
    try:
        with urllib.request.urlopen(req, timeout=40) as resp:
            return resp.status, resp.read(), resp.headers.get("Content-Type")
    except urllib.error.HTTPError as e:
        return e.code, e.read(), None


def leaf(args, name):
    with open(os.path.join(args.leaves, name), "rb") as f:
        return f.read()


def small_jpeg(args, name="healthy.jpg"):
    from PIL import Image
    im = Image.open(os.path.join(args.leaves, name)).resize((480, 360))
    buf = io.BytesIO()
    im.save(buf, "JPEG", quality=80)
    return buf.getvalue()


def status(base, tok):
    st, body, _ = call(base, "GET", "api/Farmer/leaf-check/status", token=tok)
    if st != 200:
        sys.exit(f"status failed {st}: {body}")
    return body


def top(result):
    return (result.get("nutrients") or [{}])[0].get("code")


# ------------------------------------------------------------------ basic mode

def basic_mode(base, args):
    print("Anonymous / wrong role")
    for path in ("api/Farmer/leaf-check/status", "api/Farmer/leaf-check/history", "api/Farmer/leaf-check/1"):
        st, _, _ = call(base, "GET", path)
        check(f"{path} without a token is 401", st == 401, st)
    st, _, _ = post_photo(base, None, small_jpeg(args))
    check("POST without a token is 401", st == 401, st)
    staff = staff_token(base, "qa.mdo", qa_password())
    st, _, _ = call(base, "GET", "api/Farmer/leaf-check/status", token=staff)
    check("status for staff is 403", st == 403, st)
    st, _, _ = post_photo(base, staff, small_jpeg(args))
    check("POST for staff is 403", st == 403, st)

    tok = farmer_token(base, args.phone)
    other = farmer_token(base, args.other_phone)

    print("Status")
    s0 = status(base, tok)
    check("status: limit 10", s0["dailyLimit"] == 10, s0)
    check("status: basic colour check (no key)", s0["fullCheck"] is False, s0)
    check("status: crops listed, Other last", "Paddy" in s0["crops"] and s0["crops"][-1] == "Other", s0["crops"])
    check("status: 8 MB", s0["maxPhotoBytes"] == 8 * 1024 * 1024, s0)
    if s0["leftToday"] < 9:
        sys.exit(f"farmer {args.phone} has only {s0['leftToday']} checks left today; run cleanup_task6_local.sql first.")

    print("Validation (none of these count)")
    used0 = s0["usedToday"]
    st, body, _ = post_photo(base, tok, None)
    check("no photo -> 400 with a photo error", st == 400 and "photo" in body.get("errors", {}), (st, body))
    st, body, _ = post_photo(base, tok, small_jpeg(args), crop="Poppy")
    check("unknown crop -> 400 with a crop error", st == 400 and "crop" in body.get("errors", {}), (st, body))
    st, body, _ = post_photo(base, tok, small_jpeg(args), leaf_age="middle")
    check("bad leafAge -> 400", st == 400 and "leafAge" in body.get("errors", {}), (st, body))
    st, body, _ = post_photo(base, tok, leaf(args, "fake.jpg"))
    check("a PDF named .jpg with image/jpeg -> 400 (magic bytes, not the content type)", st == 400 and "JPEG, PNG or WebP" in body.get("message", ""), (st, body))
    heic = b"\x00\x00\x00\x18ftypheic" + b"\x00" * 3000
    st, body, _ = post_photo(base, tok, heic, content_type="image/heic", filename="IMG_1.HEIC")
    check("HEIC -> 400 with the HEIC message", st == 400 and "HEIC" in body.get("message", ""), (st, body))
    big = b"\xff\xd8\xff\xe0" + b"\x00" * (8 * 1024 * 1024 + 100)
    st, body, _ = post_photo(base, tok, big)
    check("over 8 MB -> 400 '8 MB'", st == 400 and "8 MB" in body.get("message", ""), (st, str(body)[:200]))
    st, body, _ = post_photo(base, tok, b"\xff\xd8\xff\xe0garbage" * 50)
    check("a broken JPEG -> 400 'could not read'", st == 400 and "could not read" in body.get("message", ""), (st, body))
    check("validation failures are not counted", status(base, tok)["usedToday"] == used0)

    print("Basic colour check results")
    results = {}
    cases = [("healthy.jpg", None, "Paddy"), ("n_deficient.jpg", "older", "Maize"), ("interveinal.jpg", "younger", "Groundnut"),
             ("k_margin.jpg", "older", "Banana"), ("not_leaf.jpg", None, "Cotton")]
    for name, age, crop in cases:
        st, body, _ = post_photo(base, tok, leaf(args, name), crop=crop, leaf_age=age)
        check(f"{name}: 200", st == 200, (st, body))
        results[name] = body if st == 200 else {}
    for name, body in results.items():
        if body:
            check(f"{name}: labelled 'Basic colour check' (no key)", body["provider"] == "basic" and body["basicLabel"] == "Basic colour check"
                  and body["basicReason"] in ("no-key", "not-a-leaf") and body["notice"], body)
            check(f"{name}: basic disclaimer", "Basic colour check" in body["disclaimer"], body["disclaimer"])
            check(f"{name}: confidence at most 0.5", body["confidence"] <= 0.5, body["confidence"])
    h, n, fe, k, nl = (results[c[0]] for c in cases)
    check("healthy leaf -> healthy, no correctives", h.get("condition") == "healthy" and not h.get("correctives"), h)
    check("healthy leaf -> leaf colour score 4+", h.get("metrics", {}).get("lccScore", 0) >= 4, h.get("metrics"))
    check("yellow leaf, older -> N first, uniform pattern", top(n) == "N" and n["metrics"]["pattern"] == "uniform", (top(n), n.get("metrics")))
    check("yellow leaf -> urea correctives incl. the fertilizer advice link row", any(c["nutrient"] == "N" for c in n.get("correctives", [])), n.get("correctives"))
    check("interveinal, young leaves -> Fe first, interveinal pattern", top(fe) == "Fe" and fe["metrics"]["pattern"] == "interveinal", (top(fe), fe.get("metrics")))
    check("interveinal on groundnut -> the groundnut iron row", any(c["nutrient"] == "Fe" and c["forCrop"] and "1 %" in c["dose"] for c in fe.get("correctives", [])), fe.get("correctives"))
    check("brown edges, older -> K first, margin pattern", top(k) == "K" and k["metrics"]["pattern"] == "margin", (top(k), k.get("metrics")))
    check("a printed page -> not a leaf", nl.get("notALeaf") is True and nl.get("basicReason") == "not-a-leaf" and not nl.get("correctives"), nl)
    check("correctives carry TNAU sources", all("TNAU" in c["source"] or "SPIC ONE" in c["source"] for r in results.values() for c in r.get("correctives", [])))
    check("no personal fields in the result", not any(key in json.dumps(results) for key in ('"phone"', '"userId"', '"farmerUserId"', '"imagePath"')))

    print("Photo handling")
    st, body, _ = post_photo(base, tok, leaf(args, "rotated_exif.jpg"), crop="Paddy")
    check("EXIF photo: 200", st == 200, (st, body))
    if st == 200:
        st, img, ctype = get_bytes(base, f"api/Farmer/leaf-check/{body['id']}/image", tok)
        check("image: 200 JPEG for the owner", st == 200 and ctype == "image/jpeg" and img[:3] == b"\xff\xd8\xff", (st, ctype))
        check("image: no EXIF / GPS left", b"Exif" not in img and b"GPS" not in img)
        from PIL import Image
        im = Image.open(io.BytesIO(img))
        check("image: turned upright from EXIF 6 (landscape again), at most 1024 px", im.width > im.height and max(im.size) <= 1024, im.size)
        st, thumb, _ = get_bytes(base, f"api/Farmer/leaf-check/{body['id']}/image?thumb=true", tok)
        check("thumbnail: at most 240 px", st == 200 and max(Image.open(io.BytesIO(thumb)).size) <= 240)
        st, _, _ = get_bytes(base, f"api/Farmer/leaf-check/{body['id']}/image", other)
        check("image: another farmer gets 404", st == 404, st)
        st, _, _ = get_bytes(base, f"api/Farmer/leaf-check/{body['id']}/image", staff)
        check("image: staff get 403", st == 403, st)
        # the generic Uploads routes (any signed-in user) must not reach leaf photos (fix round 1, review I1)
        files = []
        for dirpath, _, names in os.walk(UPLOADS):
            files += [os.path.relpath(os.path.join(dirpath, x), os.path.dirname(UPLOADS)).replace(os.sep, "/") for x in names]
        check("photo stored in the private root, nothing under Uploads/FarmerLeaf", len(files) > 0 and not os.path.exists(SHARED_UPLOADS), files[:1])
        if files:
            rel = files[0]
            routes = ["api/Community/file/", "api/DealerFile/view/", "api/Sas/file/", "api/GuestHouseBooking/image/"]
            tries = [rel, "..%2FPrivateUploads%2F" + rel.replace("/", "%2F"), "%2E%2E%2FPrivateUploads%2F" + rel.replace("/", "%2F"),
                     "..%5CPrivateUploads%5C" + rel.replace("/", "%5C")]
            served = []
            for route in routes:
                for t in tries:
                    st, body, _ = get_bytes(base, route + t, other)
                    if st == 200 and body[:2] == b"\xff\xd8":
                        served.append(route + t)
            check("no generic file route (Community, DealerFile, Sas, GuestHouse; plain or ../ paths) serves a leaf photo", not served, served)
    st, body, _ = post_photo(base, tok, leaf(args, "big.png"), crop="Cotton", content_type="image/png", filename="leaf.png")
    check("2400 px PNG: 200, stored at most 1024 px", st == 200, (st, body))
    if st == 200:
        st, img, _ = get_bytes(base, f"api/Farmer/leaf-check/{body['id']}/image", tok)
        from PIL import Image
        check("PNG stored as a 1024 px JPEG", img[:2] == b"\xff\xd8" and max(Image.open(io.BytesIO(img)).size) == 1024)

    print("History, one check, delete")
    st, hist, _ = call(base, "GET", "api/Farmer/leaf-check/history", token=tok)
    check("history: 200, newest first", st == 200 and len(hist) >= 6 and hist[0]["createdAtUtc"] >= hist[-1]["createdAtUtc"], (st, hist[:2]))
    check("history: rows carry labels and the colour score", all(r["conditionLabel"] and "lccScore" in r for r in hist), hist[:1])
    first = results["interveinal.jpg"]
    st, one, _ = call(base, "GET", f"api/Farmer/leaf-check/{first['id']}", token=tok)
    check("one check: 200, the same result", st == 200 and one["condition"] == first["condition"] and top(one) == top(first), (st, one))
    st, _, _ = call(base, "GET", f"api/Farmer/leaf-check/{first['id']}", token=other)
    check("one check: another farmer gets 404", st == 404, st)
    st, _, _ = call(base, "DELETE", f"api/Farmer/leaf-check/{first['id']}", token=other)
    check("delete: another farmer gets 404", st == 404, st)
    before = sum(len(n) for _, _, n in os.walk(UPLOADS))
    st, _, _ = call(base, "DELETE", f"api/Farmer/leaf-check/{first['id']}", token=tok)
    check("delete own: 204", st == 204, st)
    after = sum(len(n) for _, _, n in os.walk(UPLOADS))
    check("delete: the photo file is removed", after == before - 1, (before, after))
    st, _, _ = call(base, "GET", f"api/Farmer/leaf-check/{first['id']}", token=tok)
    check("deleted: GET 404", st == 404, st)
    st, _, _ = get_bytes(base, f"api/Farmer/leaf-check/{first['id']}/image", tok)
    check("deleted: image 404", st == 404, st)
    st, hist2, _ = call(base, "GET", "api/Farmer/leaf-check/history", token=tok)
    check("deleted: not in history", all(r["id"] != first["id"] for r in hist2))
    s1 = status(base, tok)
    check("deleted checks still count today", s1["usedToday"] == used0 + 7, (used0, s1))

    print("Daily cap (10 per IST day), parallel")
    left = s1["leftToday"]
    photo = small_jpeg(args)
    for _ in range(max(0, left - 2)):
        post_photo(base, tok, photo)
    out = []

    def one():
        out.append(post_photo(base, tok, photo)[0])
    threads = [threading.Thread(target=one) for _ in range(6)]
    [t.start() for t in threads]
    [t.join() for t in threads]
    check("6 parallel checks with 2 left: exactly 2 x 200 and 4 x 429", sorted(out) == [200, 200, 429, 429, 429, 429], out)
    st, body, headers = post_photo(base, tok, photo)
    check("over the cap: 429 with message, reset time and Retry-After", st == 429 and "10 leaf checks" in body.get("message", "")
          and body.get("resetsAtUtc") and int(headers.get("Retry-After", "0")) > 0, (st, body))
    check("status: none left", status(base, tok)["leftToday"] == 0)


# ------------------------------------------------------------------ stub mode (Claude vision through the local stub)

def set_stub(args, mode):
    req = urllib.request.Request(args.stub.rstrip("/") + "/__mode", data=json.dumps({"vision": mode}).encode(), method="POST")
    urllib.request.urlopen(req, timeout=5).read()


def stub_mode(base, args):
    tok = farmer_token(base, args.stub_phone)
    s0 = status(base, tok)
    check("status: full check with the stub key", s0["fullCheck"] is True, s0)
    if s0["leftToday"] < 8:
        sys.exit("run cleanup_task6_local.sql first (the stub farmer needs 8 checks today)")
    set_stub(args, "auto")

    print("Claude vision (stub)")
    st, body, _ = post_photo(base, tok, leaf(args, "rotated_exif.jpg"), crop="Paddy", leaf_age="older")
    check("vision: 200, provider ai, no basic label", st == 200 and body["provider"] == "ai" and body["basicLabel"] is None and body["notice"] is None, (st, body))
    check("vision: the brief's disclaimer", body.get("disclaimer") == "AI suggestion from a photo. Confirm with a soil test or your MDO.", body.get("disclaimer"))
    with open(args.record, encoding="utf-8") as f:
        sent = json.load(f)
    blocks = sent["messages"][0]["content"]
    check("request: an image block then a text block", blocks[0]["type"] == "image" and blocks[1]["type"] == "text", [b["type"] for b in blocks])
    img = base64.b64decode(blocks[0]["source"]["data"])
    from PIL import Image
    check("request: base64 JPEG, at most 1024 px, no EXIF / GPS", blocks[0]["source"]["media_type"] == "image/jpeg" and img[:2] == b"\xff\xd8"
          and max(Image.open(io.BytesIO(img)).size) <= 1024 and b"Exif" not in img and b"GPS" not in img)
    check("request: the image is small (under 400 KB)", len(img) < 400_000, len(img))
    check("request: crop, the farmer's note and the metrics in the text", "Crop: Paddy" in blocks[1]["text"] and "older" in blocks[1]["text"] and "leaf colour score" in blocks[1]["text"], blocks[1]["text"])
    check("request: structured output (json_schema) and the configured model / effort", sent["output_config"]["format"]["type"] == "json_schema"
          and sent["output_config"].get("effort") == "low" and sent["model"], sent.get("output_config", {}).keys())
    check("request: nothing personal", not any(x in json.dumps(sent) for x in (args.stub_phone, "farmerUserId", "Village")))

    cases = [("n_deficient.jpg", "Maize", "older", "N"), ("interveinal.jpg", "Groundnut", "younger", "Fe"), ("k_margin.jpg", "Cotton", "older", "K")]
    for name, crop, age, expect in cases:
        st, body, _ = post_photo(base, tok, leaf(args, name), crop=crop, leaf_age=age)
        check(f"vision {name}: {expect} first with correctives", st == 200 and body["provider"] == "ai" and top(body) == expect
              and any(c["nutrient"] == expect for c in body["correctives"]) and body["advice"], (st, body))
    set_stub(args, "notleaf")
    st, body, _ = post_photo(base, tok, leaf(args, "healthy.jpg"), crop="Paddy")
    check("vision says not a leaf -> notALeaf, unclear, no correctives", st == 200 and body["notALeaf"] and body["condition"] == "unclear" and not body["correctives"], body)

    print("Fallbacks to the basic colour check")
    for mode in ("malformed", "fail", "refuse"):
        set_stub(args, mode)
        st, body, _ = post_photo(base, tok, leaf(args, "n_deficient.jpg"), crop="Maize", leaf_age="older")
        check(f"stub {mode} -> basic colour check, reason ai-failed, still N", st == 200 and body["provider"] == "basic" and body["basicReason"] == "ai-failed"
              and body["basicLabel"] == "Basic colour check" and top(body) == "N", (st, body))
    set_stub(args, "auto")
    # FarmerPortal__AiKindDailyLimit__LeafCheck = 8: the 9th AI call of the day is refused by the budget.
    st, body, _ = post_photo(base, tok, leaf(args, "k_margin.jpg"), crop="Cotton", leaf_age="older")
    check("over the LeafCheck sub-limit -> basic colour check, reason budget", st == 200 and body["provider"] == "basic" and body["basicReason"] == "budget"
          and "12 midnight" in (body.get("notice") or ""), (st, body))
    check("status: no full check while the budget is used up", status(base, tok)["fullCheck"] is False)


def kill_mode(base, args):
    tok = farmer_token(base, args.stub_phone)
    check("kill switch: status has no full check", status(base, tok)["fullCheck"] is False)
    st, body, _ = post_photo(base, tok, leaf(args, "n_deficient.jpg"), crop="Maize", leaf_age="older")
    check("kill switch: basic colour check, reason paused", st == 200 and body["provider"] == "basic" and body["basicReason"] == "paused" and "paused" in body["notice"], (st, body))


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--base", default="http://localhost:5120")
    ap.add_argument("--mode", choices=("basic", "stub", "kill"), default="basic")
    ap.add_argument("--leaves", required=True, help="folder written by make_leaves.py")
    ap.add_argument("--phone", default="6777777703")
    ap.add_argument("--other-phone", default="6163665844")
    ap.add_argument("--stub-phone", default="6999174165")
    ap.add_argument("--stub", default="http://127.0.0.1:5999")
    ap.add_argument("--record", default="")
    args = ap.parse_args()
    if not args.base.startswith(("http://localhost", "http://127.0.0.1")):
        sys.exit("local API only")
    {"basic": basic_mode, "stub": stub_mode, "kill": kill_mode}[args.mode](args.base, args)
    print()
    print("FAILED: " + ", ".join(failures) if failures else "All checks passed.")
    sys.exit(1 if failures else 0)


if __name__ == "__main__":
    main()
