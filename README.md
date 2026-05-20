# Hand Tracking Error Solution：MediaPipe `.dylib` Blocked on macOS

## Problem

macOS blocks:

```text
libmediapipe_c.dylib
````

because Apple cannot verify the plugin.

---

# Solution

## Option 1 — Allow Anyway (Recommended)

1. Click:

```text
Done
```

2. Open:

```text
System Settings
→ Privacy & Security
```

3. Scroll down and find:

```text
"libmediapipe_c.dylib" was blocked
```

4. Click:

```text
Allow Anyway
```

5. Reopen Unity and click:

```text
Open
```

---

## Option 2 — Remove macOS Quarantine (Terminal)

Run:

```bash
sudo xattr -rd com.apple.quarantine "/YourProjectPath/"
```

Example:

```bash
sudo xattr -rd com.apple.quarantine "/Users/yuqi/Desktop/UnityProject"
```

This removes Apple's quarantine restriction for the whole project.

---

# Notes

* This is a common issue with MediaPipe on macOS.
* It does **not** mean the Unity project is broken.
* Usually happens the first time `.dylib` plugins are loaded.

```
```
