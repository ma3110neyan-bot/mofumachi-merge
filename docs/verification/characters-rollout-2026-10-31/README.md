# 2026-10-31 local verification

Product/test source: commit7fda843, no product modifications in this verification.

Existing NUnit3.14 tests executed directly through NUnitTestAssemblyRunner with TestFilter.Empty and zero worker threads, using rebuilt unmodified test assemblies. This requires no testhost communication socket. The intentional failing two-case sentinel returned exit1/pass1/fail1, proving the runner does not turn a failing suite green. The sentinel is not a product test.

Actual product suites: Core125/125 and Presentation394/394 passed; total519, failure0, skip0, inconclusive0. Raw NUnit XML and receipts are stored by suite, with test assemblySHA256. The earlier standard dotnet testhost socket failures remain historical and are not counted as passing runs.

Normal Presentation, Android candidate define, Editor, and PlayMode test API builds passed with zero warnings/errors. These are compilation results, not Unity execution.

Actual Unity EditMode/PlayMode, six-character rendering/input/Sorting, Android APKs, Windows PowerShell5.1, Pixel3a performance/save/visual acceptance remain pending. No new visual acceptance is inferred. GitHub publication is not confirmed; execution approval requests were interrupted. The existing feature branch was last confirmed remotely at8bc239c before the interrupted push attempt.
