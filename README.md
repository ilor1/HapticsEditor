# HapticsEditor
Funscript (Haptics) editor/playback tool for audio files.

**Known Issues:**

This tool is currently in a pre-alpha state. 
- Only Windows OS
- Only mp3 files are supported. 
- Some longer files (hour+) might cause a crash. This might depend on available system memory.

**General:**
- Saving: Ctrl + S (there is no autosave, you have been warned!)
- Loading: Drag and drop an mp3 file. The funscript is loaded automatically as long as it is located in the same folder with the same name. (for example: MyAudio.mp3 & MyAudio.funscript)
- Connects to supported bluetooth toys automatically (refer to buttplug.io for a list of supported toys)

**Playback:**
- Play/Pause: Space
- Rewind (4x): Left Arrow
- Rewind (16x): Shift + Left Arrow
- Fast forward (4x): Right Arrow
- Fast forward (16x): Shift + Right Arrow
- Timeline scrub: Mouseover the waveform area to get a slider

**Adding and Removing points:**
- Enter point mode (on by default): 1
- Add point: Left Mousebutton
- Remove next point: Right Mousebutton
- Remove previous point: Ctrl + Right Mousebutton
- Change mode (step/linear): Scroll-wheel
- Toggle snapping: Q 

**Patterns:**
- Enter pattern mode: 2
- Paste Pattern: Left Mousebutton
- Remove next point: Right Mousebutton
- Remove previous point: Ctrl + Right Mousebutton
- Cycle through patterns: Q
- Change intensity: Scroll-wheel
- Change length: Ctrl + Scroll-wheel
- Change spacing: Alt + Scroll-wheel
- Change repeat: Shift + Scroll-wheel

**Creating patterns:**
- Create patterns: Create a new .json file at: FunScript Editor_Data\StreamingAssets\Patterns\
- Make sure the pattern you create is 1 second long (at values 0-1000). You can use pattern controls to stretch it longer/shorter 

**Metadata**
- Automatically loads and saves metadata. 
- Duration and title are read from the audio file when first creating the funscript.

**Features:**
- Allows live editing of haptics (uses Buttplug.io plugin to connect to bluetooth toys)
- Multiple toys connected simultaneously
- Separate waveforms for Left and Right channels (great for ASMR audios)
- Linear-mode: default point creation.
- Step-mode: creates a "step", by adding an extra point with the previous intensity value.
- Patterns and Custom patterns
- Snapping: Snaps the intensity to increments of 5. This is to match the Lovense toys' motor's 20 steps.
- On-Save: Automatically removes useless actions (multiple back to back actions that have the same intensity value).
- On-Save: Automatically adds actions every 30 seconds to prevent bluetooth timeouts.