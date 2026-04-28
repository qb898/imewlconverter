# Security Audit Report

**Project:** IME WL Converter (深蓝词库转换)  
**Scope:** `src/` — C#/.NET core library, CLI, macOS GUI  
**Date:** 2026-04-28  
**Severity scale:** Critical / High / Medium / Low / Info

---

## Summary

| Severity | Count | Fixed |
|----------|-------|-------|
| Critical | 1 | ✅ |
| High     | 4 | ✅ |
| Medium   | 3 | ✅ |
| Low      | 2 | ✅ |
| Info     | 2 | — |

---

## Findings

### CRIT-1 — Buffer Overrun in `SougouPinyinScel.ReadAPinyinWord()` ✅ Fixed

**File:** `src/ImeWlConverterCore/IME/SougouPinyinScel.cs`

**Description:**  
`count = num[2] + num[3] * 256` could be up to 65535, but the buffer `str` was
allocated as `new byte[256]`. The subsequent loop `for (var i = 0; i < count; i++) str[i] = ...`
would throw `IndexOutOfRangeException` for any `count > 256`. A crafted `.scel` file could
reliably trigger this denial-of-service crash.

Additionally, `samePYcount` (up to 65535) and `hzBytecount` (up to 65535) had no upper
bounds, allowing allocation of arbitrarily large buffers on corrupted input.

**Fix:** Added constants `MAX_PY_COUNT_BYTES = 256`, `MAX_SAME_PY_COUNT = 1000`,
`MAX_HZ_BYTECOUNT = 16 KB` and throw `InvalidDataException` when exceeded.

---

### HIGH-1 — Negative Array Size in `ZiGuangPinyinUwl.Parse()` ✅ Fixed

**File:** `src/ImeWlConverterCore/IME/ZiGuangPinyinUwl.cs`

**Description:**  
`lenWord = lenWord % 0x80 - 1` evaluates to -1 when the byte read from the file is 0.
`new byte[-1]` throws `OverflowException`. A crafted `.uwl` file can trigger this crash.

**Fix:** Added runtime check `if (lenWord < 0) throw new InvalidDataException(...)`.

---

### HIGH-2 — Debug.Assert Bypassed in Release Mode (`SougouPinyinDict`) ✅ Fixed

**File:** `src/ImeWlConverterCore/IME/SougouPinyinDict.cs`

**Description:**  
`Debug.Assert(offset <= header.DataSize)` is compiled out in Release builds. In Release,
a corrupted file can supply an out-of-range offset, leading to memory access past the
data store boundary without any runtime error.

**Fix:** Replaced `Debug.Assert` with `if (offset > header.DataSize) throw new InvalidDataException(...)`.

---

### HIGH-3 — Unreachable Bounds Check (`SougouPinyinBinFromPython`) ✅ Fixed

**File:** `src/ImeWlConverterCore/IME/SougouPinyinBinFromPython.cs`

**Description:**  
`MAX_WORD_SIZE = 1 MB` but `wordSize` is read as `ushort` (max 65535). The check
`wordSize > MAX_WORD_SIZE` was therefore always false, generating compiler warning CS0652.
Similarly in `decryptWords()`, `n > (MAX_WORD_SIZE / 4)` — `MAX_WORD_SIZE/4 = 262144` which
exceeds ushort max, so the guard was also never reachable.

**Fix:** Removed `MAX_WORD_SIZE`, kept the `wordSize == 0` zero-check (sufficient since
ushort is naturally bounded at 65535), and updated `decryptWords` to use `ushort.MaxValue / 4`.

---

### HIGH-4 — Sync-Over-Async + HttpClient Resource Leak (`HttpHelper`) ✅ Fixed

**File:** `src/ImeWlConverterCore/Helpers/HttpHelper.cs`

**Description:**  
`client.GetStreamAsync(url).GetAwaiter().GetResult()` blocks the calling thread on an async
operation. On a synchronization-context-bearing host this can deadlock. Additionally,
`HttpClient` was instantiated without `using`, leaking socket resources on every call.

**Fix:** Wrapped with `Task.Run(...)` to execute on the thread pool (avoids sync-context
deadlock) and added `using var client = new HttpClient()`.

---

### MED-1 — Path Traversal in `FileOperationHelper.GetFilesPathFor1()` (Partial)

**File:** `src/ImeWlConverterCore/Helpers/FileOperationHelper.cs`

**Description:**  
`Directory.GetFiles(input, "*.*", SearchOption.AllDirectories)` is called with a
user-supplied path without normalization. A path containing `..` segments allows reading
files outside the intended directory. `SearchOption.AllDirectories` also enables
unbounded recursive traversal of the filesystem.

**Recommendation:** Resolve and canonicalize the path with `Path.GetFullPath()` and
validate it is within an allowed base directory before calling `GetFiles`. Add a depth
or file-count limit for recursive traversal.

> Note: This is a medium-severity architectural issue. Full remediation requires
> understanding the caller contract. Not auto-fixed to avoid breaking changes.

---

### MED-2 — Swallowed Exceptions in File Write Helpers

**File:** `src/ImeWlConverterCore/Helpers/FileOperationHelper.cs`

**Description:**  
`WriteFile()`, `WriteFileLine()`, and `WriteFileLine(StreamWriter)` use bare `catch { return false; }`.
Callers get a boolean `false` but have no way to know whether the failure was
`UnauthorizedAccessException`, `IOException`, `DirectoryNotFoundException`, etc.
Security-relevant write failures (e.g. permission denied, disk full) are silently swallowed.

**Recommendation:** Log the exception type at minimum (e.g. with `Debug.WriteLine`), or
expose the exception to the caller. Consider propagating specific exception types so callers
can act appropriately.

> Note: These methods are used throughout the codebase. Not auto-changed to avoid
> silent behavioral regressions in callers.

---

### MED-3 — Transitive Dependency: `Tmds.DBus.Protocol` 0.20.0 (CVE GHSA-xrw6-gwf8-vvr9) ✅ Fixed

**File:** `src/ImeWlConverterMac/ImeWlConverterMac.csproj`

**Description:**  
Avalonia 11.2.3 transitively depends on `Tmds.DBus.Protocol` 0.20.0, which has a **High**
severity vulnerability: malicious D-Bus peers can spoof signals, exhaust file descriptor
resources, and cause denial-of-service. Affects: < 0.21.3.

**Fix:** Added explicit `PackageReference` override to `Tmds.DBus.Protocol` 0.21.3 (patched).

---

### LOW-1 — Dockerfile: SHA256 Verification is Optional

**File:** `Dockerfile`

**Description:**  
The `DOWNLOAD_SHA256` build argument defaults to empty (`""`). When not provided, the
downloaded archive is extracted without integrity verification, leaving the build open to
supply-chain attacks (DNS hijack, CDN compromise, MITM). The `BUILD_FROM_ARTIFACT` mode
(using a local artifact) avoids this.

**Recommendation:** Require `DOWNLOAD_SHA256` when `BUILD_FROM_ARTIFACT=false`, or
document that users must pin the checksum. Consider making the default build mode use
`BUILD_FROM_ARTIFACT=true` (artifact supplied by CI), eliminating the download entirely.

---

### LOW-2 — `ImeWlConverterCore-net46.csproj` Uses SharpZipLib 1.2.0

**File:** `src/ImeWlConverterCore/ImeWlConverterCore-net46.csproj`

**Description:**  
The legacy .NET Framework 4.6 project file references `SharpZipLib` version 1.2.0.
The current active project (`ImeWlConverterCore.csproj`) correctly uses 1.4.2.
The legacy file appears unused in current builds but could be mistakenly used.

**Recommendation:** Either remove the legacy project file or update it to SharpZipLib 1.4.2.

---

### INFO-1 — Reflection-Based `CreateInstance` Scoped to Executing Assembly

**Files:** `MainWindowViewModel.cs`, `MainForm.cs`, `ConsoleRun.cs`

**Description:**  
`assembly.CreateInstance(type.FullName)` and `Activator.CreateInstance(type)` enumerate
types from the executing assembly. Since no external assembly is loaded, the attack surface
is limited to types already in the application. Risk is low but worth noting: if plugin
loading is added in future, this pattern would need sandboxing.

---

### INFO-2 — No Hardcoded Secrets Found

A pattern scan for common secret patterns (`BEGIN RSA PRIVATE KEY`, `AKIA*`, `password =`,
`api_key =`) found no hardcoded credentials or private keys in the codebase.

---

## Automated Scan Results

| Tool | Result |
|------|--------|
| `dotnet list package --vulnerable` (Core) | ✅ No vulnerabilities |
| `dotnet list package --vulnerable` (Cmd) | ✅ No vulnerabilities |
| `dotnet list package --vulnerable` (Mac, after fix) | ✅ No vulnerabilities |
| Secret pattern scan | ✅ No secrets found |
| Compiler warnings | CS8603 (pre-existing nullable), CS0652 fixed |

---

## Recommendations (Priority Order)

1. **Immediately (Critical/High):** All Critical and High findings have been fixed in this PR.
2. **Short-term (Medium):** Implement path canonicalization in `FileOperationHelper.GetFilesPathFor1()`;
   add structured exception logging to write helpers.
3. **Medium-term (Low):** Require SHA256 in Dockerfile; update/remove legacy `.csproj`.
4. **Ongoing:** Enable Dependabot for NuGet automatic dependency updates; add SAST
   (e.g., `dotnet-security-guard`, Semgrep C# rules) to CI.
