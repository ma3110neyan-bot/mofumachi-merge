"""One-character material/browser check, never native/game/device acceptance."""
from pathlib import Path
import json
import sys
from playwright.sync_api import sync_playwright
root=Path(__file__).resolve().parents[2]
character=sys.argv[2] if len(sys.argv)>2 else 'momo'
out=Path(sys.argv[1]);out.mkdir(parents=True,exist_ok=True)
html=root/f'References/Characters/{character}-authored-parts-2026-10-30/material-review.html'
errors=[];requests=[];samples=[]
with sync_playwright() as p:
    browser=p.chromium.launch(executable_path='/usr/bin/chromium',headless=True,args=['--no-sandbox'])
    try:
        page=browser.new_page(viewport={'width':980,'height':1120})
        page.on('pageerror',lambda e:errors.append(str(e)))
        page.on('request',lambda r:requests.append(r.url) if r.url.startswith(('http:','https:')) else None)
        page.set_content(html.read_text(),wait_until='load');page.wait_for_function('window.characterReviewReady === true')
        page.evaluate("window.setCharacterPose(0,'idle','front')")
        assert page.locator('#before').evaluate('c=>c.toDataURL()')==page.locator('#after').evaluate('c=>c.toDataURL()')
        for d in ['left','right','back']:
            for m in ['idle','walk','talk','greet']:
                page.evaluate('([d,m])=>window.setCharacterPose(.6,m,d)',[d,m])
                state=page.evaluate('window.characterReviewState');assert state['parts']==7 and max(f['y'] for f in state['feet'])==0
                assert not state['unityVerified'] and not state['pixelVerified']
                if m=='idle':
                    gaps=page.locator('#after').evaluate('c=>{const p=c.getContext("2d").getImageData(170,255,20,86).data;let empty=0;for(let y=0;y<86;y++){let n=0;for(let x=0;x<20;x++)if(p[(y*20+x)*4+3]>40)n++;if(n===0)empty++;}return empty;}')
                    assert gaps==0,(d,'head must meet the collar, not float above it',gaps)
                page.locator('#after').screenshot(path=str(out/f'material-{d}-{m}.png'))
            page.evaluate("(d)=>window.setCharacterPose(.6,'idle',d,true)",d)
            page.locator('#after').screenshot(path=str(out/f'material-{d}-blink.png'))
        for i in range(37):
            t=i*.5;d='left' if t<5 else 'right' if t<10 else 'back' if t<14 else 'right';m='idle' if t<1.5 or 4<t<5 or 9<t<10 or 13<t<14 else 'walk' if t<14 else 'greet' if t<16 else 'talk'
            page.evaluate('([t,m,d])=>window.setCharacterPose(t,m,d)',[t,m,d]);s=page.evaluate('window.characterReviewState');assert max(f['y'] for f in s['feet'])==0;samples.append(s)
        page.set_viewport_size({'width':390,'height':870});assert page.evaluate('document.documentElement.scrollWidth<=window.innerWidth')
        page.screenshot(path=str(out/'material-review-mobile.png'),full_page=True)
        assert not errors,errors;assert not requests,requests
    finally:browser.close()
report={'scope':character+' material/browser only','front_exact_original':True,'three_directions_four_modes':True,'closed_heads_rendered':True,'samples':samples,'errors':errors,'external_requests':requests,'unity_verified':False,'pixel_verified':False,'adopted':False}
(out/'browser-results.json').write_text(json.dumps(report,indent=2)+'\n')
print(character+' material browser passed: exact formal front, 3 directions/4 modes, blink sources, 37 fixed-time samples, 390px layout, no script errors. Not Unity/Pixel.')
