# Third-party notices

## Forge Oracle

Forge Oracle Client builds on the research published by Forge Oracle by papptimus: the web tool at
<https://papptimus.github.io/Forge-Oracle/> and its source at <https://github.com/papptimus/Forge-Oracle>. Its
description of the seed table and its reference implementation
(`hash` and `rollNext` in `index.html`, commit `4ebded44f6f2288387b5f6aa9fecc32428422a22`) were used to produce the
golden test vectors in `tests/ForgeOracle.Core.Tests/Fixtures/golden-vectors.json`. The generator
(`tools/golden/generate-vectors.mjs`) reads those functions from a local checkout; no Forge Oracle source file is
redistributed. The C# engine was written from the decompiled game code and checked against those vectors.

```
MIT License

Copyright (c) 2026 papptimus

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

## ValheimSaveParser

ValheimSaveParser by Melichor (<https://melichor.github.io/ValheimSaveParser/>,
<https://github.com/Melichor/ValheimSaveParser>) was consulted as an independent analysis of the same mechanics. No
code from it is included.

## Valheim, BepInEx, Harmony

Valheim is © Iron Gate AB. Its assemblies are referenced at build time from the local installation and are never
copied into this repository or the release package. BepInEx (LGPL-2.1) and HarmonyX (MIT) are runtime dependencies
installed by the BepInExPack, not bundled.

## Other mods

ImpactfulSkills, PotentialReforged and ForesightForge are not bundled or referenced at build time. Their public or
internal static members are looked up by reflection at run time, only when the mod is loaded.
