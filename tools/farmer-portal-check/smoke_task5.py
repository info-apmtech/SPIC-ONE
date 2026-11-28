"""
Farmer portal Task 5 smoke test: SPIC AI for farmers (api/Farmer/assistant), against a LOCAL API and the LOCAL
spicone_dev database only.

Keyword mode (no Anthropic key; the API as for the other smoke tests):

    $env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5432;Database=spicone_dev;Username=postgres;Password=1234"
    dotnet run --project SpicAPI --no-launch-profile --urls http://localhost:5120     (ASPNETCORE_ENVIRONMENT=Development)
    python tools/farmer-portal-check/smoke_task5.py

Stub mode (the Anthropic path against tools/farmer-portal-check/fake_anthropic.py, never the real API):

    python tools/farmer-portal-check/fake_anthropic.py --record <scratch>/last.json
    $env:Assistant__AnthropicApiKey = "local-stub"; $env:Assistant__Endpoint = "http://127.0.0.1:5999/v1/messages"
    dotnet run --project SpicAPI --no-launch-profile --urls http://localhost:5120
    python tools/farmer-portal-check/smoke_task5.py --mode stub --record <scratch>/last.json

Budget mode (fix round 1): the stub API above plus $env:FarmerPortal__AssistantGlobalDailyLimit = "3", after
cleanup_task5_local.sql: python tools/farmer-portal-check/smoke_task5.py --mode budget --limit 3
Kill switch: the stub API plus $env:FarmerPortal__AssistantEnabled = "false":
    python tools/farmer-portal-check/smoke_task5.py --mode kill --record <scratch>/last.json

Keyword mode uses up the daily allowance of the cap farmer (6163665844) for today's IST day;
cleanup_task5_local.sql removes the local test farmers' chats (and so resets it).
"""
import argparse
import json
import os
import sys
import threading

sys.path.insert(0, os.path.dirname(__file__))
from smoke_task3 import call, check, failures, farmer_token, staff_token, qa_password, walk_keys  # noqa: E402

LABEL = "Basic answer from the SPIC library"


def status(base, tok):
    st, body, _ = call(base, "GET", "api/Farmer/assistant/status", token=tok)
    if st != 200:
        sys.exit(f"status failed {st}: {body}")
    return body


def ask(base, tok, question, conversation_id=None):
    payload = {"question": question}
    if conversation_id:
        payload["conversationId"] = conversation_id
    st, body, secs = call(base, "POST", "api/Farmer/assistant/ask", payload, token=tok)
    return st, body, secs


def keyword_mode(base, args):
    print("Anonymous / wrong role")
    for path in ("api/Farmer/assistant/status", "api/Farmer/assistant/conversations"):
        st, _, _ = call(base, "GET", path)
        check(f"{path} without a token is 401", st == 401, st)
    st, _, _ = call(base, "POST", "api/Farmer/assistant/ask", {"question": "hi"})
    check("ask without a token is 401", st == 401, st)
    staff = staff_token(base, "qa.mdo", qa_password())
    st, _, _ = call(base, "GET", "api/Farmer/assistant/status", token=staff)
    check("status for staff is 403", st == 403, st)
    st, _, _ = call(base, "POST", "api/Farmer/assistant/ask", {"question": "hi"}, token=staff)
    check("ask for staff is 403", st == 403, st)

    tok = farmer_token(base, args.phone)
    other = farmer_token(base, args.cap_phone)
    tamil = farmer_token(base, args.tamil_phone)

    print("Status")
    s0 = status(base, tok)
    check("status: limit 30", s0["dailyLimit"] == 30, s0)
    check("status: basic answers (no key)", s0["fullAnswers"] is False, s0)
    check("status: English profile", s0["language"] == "en" and s0["languageName"] == "English", s0)
    check("status: four starters, paddy first", len(s0["starters"]) == 4 and "paddy" in s0["starters"][0].lower(), s0["starters"])
    check("status: max 1000 letters", s0["maxQuestionChars"] == 1000, s0)
    st_t = status(base, tamil)
    check("status: Tamil profile", st_t["language"] == "ta" and st_t["languageName"] == "Tamil", st_t)

    print("Validation")
    st, body, _ = ask(base, tok, "   ")
    check("empty question is 400", st == 400 and "type your question" in body.get("message", ""), (st, body))
    st, body, _ = ask(base, tok, "a" * 1001)
    check("1001 letters is 400", st == 400 and "1000" in body.get("message", ""), (st, body))
    st, body, _ = ask(base, tok, "a" * 1000)
    check("1000 letters is answered", st == 200, (st, body))
    if st == 200:
        call(base, "DELETE", f"api/Farmer/assistant/conversations/{body['conversationId']}", token=tok)
    st, body, _ = ask(base, tok, "hello", conversation_id=999999999)
    check("unknown conversation is 404", st == 404, (st, body))

    print("Ask (basic answer)")
    used_before = status(base, tok)["usedToday"]
    st, a1, secs = ask(base, tok, "How much DAP should I apply for paddy?")
    check("ask: 200", st == 200, (st, a1))
    if st != 200:
        return
    ans = a1["answer"]
    check("ask: provider keyword", ans["provider"] == "keyword", ans)
    check("ask: labelled as a basic answer", ans["text"].startswith(LABEL), ans["text"][:120])
    check("ask: library sources (the DAP / paddy items)", len(ans["sources"]) >= 1 and any("DAP" in s["title"] for s in ans["sources"]), ans["sources"])
    check("ask: follow-ups", len(ans["suggestions"]) == 3, ans["suggestions"])
    check("ask: question and answer times are UTC", a1["question"]["createdAtUtc"].endswith("Z") or "+00:00" in a1["question"]["createdAtUtc"], a1["question"]["createdAtUtc"])
    check("ask: left today goes down by one", a1["leftToday"] == 30 - used_before - 1, (a1["leftToday"], used_before))
    check("ask: title is the question", a1["conversationTitle"] == "How much DAP should I apply for paddy?", a1["conversationTitle"])
    print(f"    (answered in {secs:.2f}s)")
    cid = a1["conversationId"]

    st, a2, _ = ask(base, tok, "Which pesticide should I spray for stem borer?", conversation_id=cid)
    check("follow-up: same conversation", st == 200 and a2["conversationId"] == cid, (st, a2))
    check("follow-up: pesticide question gets the label and safety line", "follow the label dose exactly" in a2["answer"]["text"], a2["answer"]["text"])

    st, convs, _ = call(base, "GET", "api/Farmer/assistant/conversations", token=tok)
    row = next((c for c in convs if c["id"] == cid), None) if st == 200 else None
    check("list: newest first with 2 questions", st == 200 and convs and convs[0]["id"] == cid and row and row["questionCount"] == 2, convs[:2] if st == 200 else st)
    st, detail, _ = call(base, "GET", f"api/Farmer/assistant/conversations/{cid}", token=tok)
    check("detail: 4 messages, question / answer in order", st == 200 and [m["role"] for m in detail["messages"]] == ["user", "assistant", "user", "assistant"], detail)
    check("detail: sources kept", st == 200 and len(detail["messages"][1]["sources"]) >= 1, detail)
    bad = [p for p, k in walk_keys(detail) if k in ("phone", "mobile", "userId", "farmerUserId", "village", "name")]
    check("detail: no personal fields", not bad, bad)

    print("Another farmer")
    st, _, _ = call(base, "GET", f"api/Farmer/assistant/conversations/{cid}", token=other)
    check("other farmer: detail 404", st == 404, st)
    st, _, _ = ask(base, other, "steal", conversation_id=cid)
    check("other farmer: ask into it 404", st == 404, st)
    st, _, _ = call(base, "DELETE", f"api/Farmer/assistant/conversations/{cid}", token=other)
    check("other farmer: delete 404", st == 404, st)
    st, convs2, _ = call(base, "GET", "api/Farmer/assistant/conversations", token=other)
    check("other farmer: list never shows it", st == 200 and all(c["id"] != cid for c in convs2), st)

    print("Language")
    st, t1, _ = ask(base, tamil, "How do I control yellow leaves in paddy?")
    check("Tamil farmer: basic answer says English only", st == 200 and "English only" in t1["answer"]["text"] and "Tamil" in t1["answer"]["text"], t1)

    print("Delete")
    used = status(base, tok)["usedToday"]
    st, _, _ = call(base, "DELETE", f"api/Farmer/assistant/conversations/{cid}", token=tok)
    check("delete: 204", st == 204, st)
    st, _, _ = call(base, "GET", f"api/Farmer/assistant/conversations/{cid}", token=tok)
    check("delete: detail 404 afterwards", st == 404, st)
    check("delete: the questions still count today", status(base, tok)["usedToday"] == used, used)

    print("Daily cap (cap farmer)")
    s = status(base, other)
    used = s["usedToday"]
    target = 26
    while used < target:
        st, _, _ = ask(base, other, f"[smoke] question {used + 1}")
        if st != 200:
            break
        used += 1
    if used == target:
        results = []
        lock = threading.Lock()

        def one(i):
            st_, _, _ = ask(base, other, f"[smoke] parallel {i}")
            with lock:
                results.append(st_)

        threads = [threading.Thread(target=one, args=(i,)) for i in range(8)]
        for t in threads:
            t.start()
        for t in threads:
            t.join()
        check("cap: 8 parallel asks at 26 used -> exactly 4 answered, 4 refused", sorted(results) == [200] * 4 + [429] * 4, sorted(results))
    else:
        print(f"    (cap farmer already at {used} today; parallel check skipped)")
    st, body, _ = ask(base, other, "[smoke] one more")
    check("cap: 429 at the limit", st == 429, (st, body))
    if st == 429:
        check("cap: friendly message", "30 questions today" in body["message"] and "midnight" in body["message"], body)
        check("cap: retry-after until IST midnight", 0 < body["retryAfterSeconds"] <= 86400 and body["dailyLimit"] == 30, body)
        check("cap: resets at 18:30 UTC (IST midnight)", "T18:30:00" in body["resetsAtUtc"], body["resetsAtUtc"])
    s = status(base, other)
    check("cap: status shows 0 left, 30 used", s["leftToday"] == 0 and s["usedToday"] == 30, s)


def stub_mode(base, args):
    if not args.record:
        sys.exit("--record <file> (the fake_anthropic.py --record file) is required in stub mode")
    tok = farmer_token(base, args.tamil_phone)
    s = status(base, tok)
    check("stub: full answers on", s["fullAnswers"] is True, s)
    st, a1, _ = ask(base, tok, "Why are my paddy leaves turning yellow?")
    check("stub: 200", st == 200, (st, a1))
    if st != 200:
        return
    ans = a1["answer"]
    check("stub: provider anthropic", ans["provider"] == "anthropic", ans)
    check("stub: SUGGESTIONS line stripped into 3 chips", "SUGGESTIONS" not in ans["text"] and len(ans["suggestions"]) == 3, ans)
    check("stub: cited library item is a source", any("Urea" in x["title"] for x in ans["sources"]) and ans.get("sourcesRelated") is False, ans)
    with open(args.record, encoding="utf-8") as f:
        body = json.load(f)
    system = body["system"]
    last = body["messages"][-1]["content"][0]["text"]
    check("stub: model from Assistant:Model (default kept)", body["model"] == "claude-sonnet-5", body["model"])
    check("stub: farmer answer length 1500", body["max_tokens"] == 1500, body["max_tokens"])
    check("stub: system prompt asks for Tamil", "in Tamil (Tamil script)" in system, system[:300])
    check("stub: context has crops, district and no soil test", "Crops: Paddy, Banana" in last and "district" in last, last[:400])
    raw = json.dumps(body, ensure_ascii=False)
    check("stub: no farmer name or phone sent", "Smoke Test Farmer" not in raw and args.tamil_phone not in raw)
    check("stub: no thinking or sampling fields sent", "\"thinking\"" not in raw and "temperature" not in raw)
    check("stub: low effort for farmer answers (fix round 1, M7)", body.get("output_config") == {"effort": "low"}, body.get("output_config"))

    st, a2, _ = ask(base, tok, "How much urea per acre?", conversation_id=a1["conversationId"])
    with open(args.record, encoding="utf-8") as f:
        body2 = json.load(f)
    roles = [m["role"] for m in body2["messages"]]
    check("stub: follow-up sends the history (user, assistant, user)", st == 200 and roles == ["user", "assistant", "user"], roles)

    st, f1, _ = ask(base, tok, "[fail] what now?", conversation_id=a1["conversationId"])
    check("stub: an API failure falls back to the labelled basic answer", st == 200 and f1["answer"]["provider"] == "keyword"
          and f1["answer"]["text"].startswith(LABEL), f1)
    st, r1, _ = ask(base, tok, "[refuse] something")
    check("stub: a refusal falls back to the labelled basic answer", st == 200 and r1["answer"]["provider"] == "keyword", r1)

    # The English farmer with the demo soil report and fertilizer runs: ratings and the advice summary are sent.
    tok2 = farmer_token(base, args.phone)
    st, _, _ = ask(base, tok2, "What should I do for my soil?")
    with open(args.record, encoding="utf-8") as f:
        body3 = json.load(f)
    last3 = body3["messages"][-1]["content"][0]["text"]
    check("stub: soil report ratings (no values) and the fertilizer summary are sent",
          st == 200 and "Latest SPIC soil test (ratings only" in last3 and "Last fertilizer advice in the app" in last3
          and "English," in body3["system"], last3[:600])
    check("stub: soil values are not sent", "180" not in last3.split("Latest SPIC soil test")[1].split("\n")[0], last3)


def budget_mode(base, args):
    """API with the stub key and FarmerPortal__AssistantGlobalDailyLimit=<args.limit>, after cleanup_task5_local.sql
    (no AI usage rows today). Parallel asks must give exactly <limit> AI answers; the rest are labelled basic answers
    with the gentle notice."""
    tok = farmer_token(base, args.tamil_phone)
    s = status(base, tok)
    check("budget: full answers before the limit", s["fullAnswers"] is True, s)
    n = args.limit + 3
    results = []
    lock = threading.Lock()

    def one(i):
        st_, body_, _ = ask(base, tok, f"[smoke] budget {i}: why are my paddy leaves yellow?")
        with lock:
            results.append((st_, body_))

    threads = [threading.Thread(target=one, args=(i,)) for i in range(n)]
    for t in threads:
        t.start()
    for t in threads:
        t.join()
    ok = [b for st_, b in results if st_ == 200]
    full = [b for b in ok if b["answer"]["provider"] == "anthropic"]
    basic = [b for b in ok if b["answer"]["provider"] == "keyword"]
    check(f"budget: {n} parallel asks -> exactly {args.limit} AI answers", len(ok) == n and len(full) == args.limit, [(st_, b.get("answer", {}).get("provider") if isinstance(b, dict) else b) for st_, b in results])
    check("budget: the rest are labelled basic answers", len(basic) == n - args.limit and all(b["answer"]["text"].startswith(LABEL) for b in basic))
    check("budget: with the gentle notice", all("Many farmers are asking SPIC AI today" in (b.get("notice") or "") for b in basic), [b.get("notice") for b in basic])
    check("budget: AI answers carry no notice", all(not b.get("notice") for b in full))
    s = status(base, tok)
    check("budget: status says basic answers now", s["fullAnswers"] is False, s)
    st, one_more, _ = ask(base, tok, "[smoke] budget after the limit")
    check("budget: still a 200 basic answer (not a 429)", st == 200 and one_more["answer"]["provider"] == "keyword", (st, one_more))


def kill_mode(base, args):
    """API with the stub key and FarmerPortal__AssistantEnabled=false: never calls the AI."""
    before = os.path.getmtime(args.record) if args.record and os.path.exists(args.record) else None
    tok = farmer_token(base, args.tamil_phone)
    s = status(base, tok)
    check("kill switch: status says basic answers", s["fullAnswers"] is False, s)
    st, body, _ = ask(base, tok, "[smoke] kill switch: why are my paddy leaves yellow?")
    check("kill switch: labelled basic answer", st == 200 and body["answer"]["provider"] == "keyword" and body["answer"]["text"].startswith(LABEL), (st, body))
    check("kill switch: paused notice", st == 200 and "paused" in (body.get("notice") or ""), body.get("notice") if st == 200 else st)
    after = os.path.getmtime(args.record) if args.record and os.path.exists(args.record) else None
    check("kill switch: the (stub) AI was not called", before == after, (before, after))


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--base", default="http://localhost:5120")
    ap.add_argument("--mode", choices=("keyword", "stub", "budget", "kill"), default="keyword")
    ap.add_argument("--limit", type=int, default=3, help="budget mode: FarmerPortal__AssistantGlobalDailyLimit the API runs with")
    ap.add_argument("--record", default="")
    ap.add_argument("--phone", default="6777777703", help="English local test farmer with the demo soil report")
    ap.add_argument("--tamil-phone", default="6999174165", help="Tamil local test farmer")
    ap.add_argument("--cap-phone", default="6163665844", help="local test farmer whose daily allowance is used up")
    args = ap.parse_args()
    host = args.base.split("//")[-1].split(":")[0].split("/")[0]
    if host not in ("localhost", "127.0.0.1"):
        sys.exit("smoke_task5.py runs against a LOCAL API only")
    {"keyword": keyword_mode, "stub": stub_mode, "budget": budget_mode, "kill": kill_mode}[args.mode](args.base, args)
    print()
    print("FAILED: " + ", ".join(failures) if failures else "All checks passed.")
    sys.exit(1 if failures else 0)


if __name__ == "__main__":
    main()
