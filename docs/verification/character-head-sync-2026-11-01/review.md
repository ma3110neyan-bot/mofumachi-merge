# Head/body scoped review

Fresh read-only reviewer `/root/head_body_sync_review_nov01`. Basef2b3133 to working head-only changes.

One Important finding: selecting median X and extreme Y can return transparent coordinates. Reproduced on all6 IDs against46 cropped/masked source-alpha fixtures; source testsRED6 failures while10 existing maths checks passed. Fixed by selecting a genuinely opaque pixel on the extreme contour and using its texture pixel centre. Full focused16/16 green after fix; both open/closed head source registrations are covered. No further independent review dispatched.

No Critical/Minor findings reported. Body-frame composition, current angle/scale/UV preservation, closed-head selection, old attachment reconstruction and fresh-run/source/ownership guards were reviewed without additional Important issues.

Declined: actual Unity render, six-character visual naturalness, packed edge contact, Pixel/FPS, screenshot APK identity. Ruling: none of those are inferred from pure/API/synthetic checks. Native one-method432-sample/36-image handoff remains required before formal fixed/zero-failure declaration. Whole-branch review did not cover later handoff prose; prose was self-checked.
