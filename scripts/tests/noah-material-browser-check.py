"""Focused offline MATERIAL review smoke test, never Unity/Pixel acceptance."""
from pathlib import Path
from playwright.sync_api import sync_playwright
import json
import sys

root = Path(__file__).resolve().parents[2]
html = root / "References/Characters/noah-authored-parts-2026-10-29/noah-material-review.html"
output = Path(sys.argv[1])
output.mkdir(parents=True, exist_ok=True)
errors, external_requests = [], []
checks = []
with sync_playwright() as p:
    browser = p.chromium.launch(executable_path="/usr/bin/chromium", headless=True, args=["--no-sandbox"])
    try:
        page = browser.new_page(viewport={"width": 1150, "height": 1500})
        page.on("pageerror", lambda error: errors.append(str(error)))
        page.on("request", lambda request: external_requests.append(request.url) if request.url.startswith(("http:", "https:")) else None)
        # Managed Linux Chromium blocks file: navigation. This tests the same
        # self-contained bytes; it does not certify Windows file-open behavior.
        page.set_content(html.read_text(), wait_until="load")
        page.wait_for_function("window.reviewReady === true")
        assert page.locator("#original").evaluate("c => c.toDataURL()") == page.locator("#direction").evaluate("c => c.toDataURL()")
        checks.append("front shows the exact original canvas, not the generated front")
        directions = []
        for direction in ["left", "right", "back"]:
            page.locator(f'[data-direction="{direction}"]').click()
            directions.append(page.locator("#direction").evaluate("c => c.toDataURL()"))
            assert page.locator("#direction-label").inner_text().endswith("素材候補")
        assert len(set(directions)) == 3
        checks.append("three candidate direction controls render distinct materials")
        page.locator("#size").select_option("320")
        for mode in ["idle", "walk", "talk", "greet"]:
            page.evaluate("([t,m]) => window.setNoahReviewPose(t,m)", [.7, mode])
            state = page.evaluate("window.reviewState")
            assert state["mode"] == mode
            assert all(foot["y"] <= 0 for foot in state["feet"])
            assert max(foot["y"] for foot in state["feet"]) == 0
            page.locator("#rig").screenshot(path=str(output / f"material-rig-{mode}.png"))
        checks.append("four material poses draw with a grounded foot")
        page.evaluate("window.setNoahReviewPose(0,'idle')")
        page.screenshot(path=str(output / "material-review-desktop.png"), full_page=True)
        page.locator("#pause").click()
        page.wait_for_timeout(180)
        a = page.evaluate("window.reviewState.clock")
        page.wait_for_timeout(180)
        assert page.evaluate("window.reviewState.clock") > a
        assert max(foot['y'] for foot in page.evaluate("window.reviewState.feet")) == 0
        page.locator("#pause").click()
        a = page.evaluate("window.reviewState.clock")
        page.wait_for_timeout(180)
        assert page.evaluate("window.reviewState.clock") == a
        checks.append("pause and resume controls change the actual review clock")
        page.set_viewport_size({"width": 390, "height": 870})
        page.locator("#size").select_option("160")
        assert page.evaluate("document.documentElement.scrollWidth <= window.innerWidth")
        page.screenshot(path=str(output / "material-review-narrow.png"), full_page=True)
        checks.append("390px review layout has no horizontal overflow; not device testing")
        assert not errors, errors
        assert not external_requests, external_requests
        checks.append("no browser errors or external network requests")
    finally:
        browser.close()
report = {"scope": "offline material/browser preview only", "checks": checks,
          "browser_errors": errors, "external_requests": external_requests,
          "unity_verified": False, "pixel_verified": False, "adopted": False,
          "windows_file_open_verified": False}
(output / "browser-results.json").write_text(json.dumps(report, indent=2) + "\n")
print(json.dumps(report, indent=2))
