"""Scoped browser material check. Never native/game/device acceptance."""
from pathlib import Path
import json
import sys
from playwright.sync_api import sync_playwright

root = Path(__file__).resolve().parents[2]
html = root / "References/Characters/noah-directional-parts-2026-10-30/noah-direction-review.html"
output = Path(sys.argv[1])
output.mkdir(parents=True, exist_ok=True)
errors, requests, checks, samples = [], [], [], []
with sync_playwright() as p:
    browser = p.chromium.launch(executable_path="/usr/bin/chromium", headless=True, args=["--no-sandbox"])
    try:
        page = browser.new_page(viewport={"width": 1180, "height": 1200})
        page.on("pageerror", lambda e: errors.append(str(e)))
        page.on("request", lambda r: requests.append(r.url) if r.url.startswith(("http:", "https:")) else None)
        page.set_content(html.read_text(), wait_until="load")
        page.wait_for_function("window.directionReviewReady === true")
        page.locator('[data-facing="front"]').click()
        assert page.locator('#original').evaluate('c=>c.toDataURL()') == page.locator('#rig').evaluate('c=>c.toDataURL()')
        assert page.evaluate('window.directionReviewState.partCount') == 0
        checks.append("front is exact original source, explicitly not an independent front rig")
        for direction in ['left', 'right', 'back']:
            for size in ['180', '320']:
                page.locator('#size').select_option(size)
                for mode in ['idle', 'walk', 'talk', 'greet'] if size == '320' else ['idle']:
                    page.evaluate('([t,m,d])=>window.setNoahDirectionPose(t,m,d)', [.7, mode, direction])
                    s = page.evaluate('window.directionReviewState')
                    assert s['partCount'] == 8 and s['facing'] == direction and s['mode'] == mode
                    assert max(foot['y'] for foot in s['feet']) == 0
                    assert not s['gameIntegrated'] and not s['pixelVerified']
                    page.locator('#rig').screenshot(path=str(output / f'material-{direction}-{mode}-{size}.png'))
        checks.append("three directions render four rigid-part poses and grounded feet at two review sizes")
        page.locator('#size').select_option('320')
        for i in range(37):
            t=i*.5
            page.evaluate('(t)=>window.setNoahDirectionSequence(t)', t)
            s=page.evaluate('window.directionReviewState')
            samples.append({'sample_seconds': t, 'direction': s['facing'], 'mode': s['mode'], 'feet': s['feet']})
            assert max(f['y'] for f in s['feet']) == 0
            page.locator('#rig').screenshot(path=str(output / f'sequence-{i:03}.png'))
        assert {'left','right','back'} == {s['direction'] for s in samples}
        assert samples[-1]['mode'] == 'talk'
        checks.append("37 fixed-time browser samples cover18seconds of idle/walk/stop/turn/away/greet/talk; not real-time FPS")
        page.locator('#sequence').click()
        page.wait_for_timeout(250)
        a=page.evaluate('window.directionReviewState.clock')
        page.wait_for_timeout(250)
        assert page.evaluate('window.directionReviewState.clock') > a
        page.locator('#pause').click()
        a=page.evaluate('window.directionReviewState.clock')
        page.wait_for_timeout(180)
        assert page.evaluate('window.directionReviewState.clock') == a
        checks.append("actual browser clock runs and pauses")
        page.evaluate("window.setNoahDirectionPose(0,'idle','back')")
        page.screenshot(path=str(output / 'material-review-desktop.png'),full_page=True)
        page.set_viewport_size({'width':390,'height':870})
        page.locator('#size').select_option('180')
        assert page.evaluate('document.documentElement.scrollWidth<=window.innerWidth')
        page.screenshot(path=str(output / 'material-review-narrow.png'),full_page=True)
        checks.append("390px browser layout has no horizontal overflow; not Pixel testing")
        assert not errors, errors
        assert not requests, requests
        checks.append("no JavaScript errors or external requests")
    finally:
        browser.close()
report={'scope':'offline Noah material/browser review only','checks':checks,
        'samples':samples,'browser_errors':errors,'external_requests':requests,
        'game_integrated':False,'unity_verified':False,'apk_generated':False,'pixel_verified':False,
        'adopted':False,'windows_file_open_verified':False}
(output/'browser-results.json').write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps({k:v for k,v in report.items() if k!='samples'},indent=2))
