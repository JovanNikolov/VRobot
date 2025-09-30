# VRobot

Virtual reality controlled robot using Unity and Raspberry Pi.

## Project Structure

```
VRobot/
├── Assets/Scripts/          # Unity C# scripts
└── RaspberryPiScripts/      # Python scripts for Raspberry Pi
```

## Requirements

- Unity 2021.3 LTS or later
- VR headset
- Raspberry Pi 4 or later
- Python 3.7+

## Setup

### Unity
1. Clone the repository
2. Open in Unity Hub
3. Configure VR settings in Project Settings → XR Plugin Management

### Raspberry Pi
1. Copy `RaspberryPiScripts/` to your Raspberry Pi
2. Install dependencies:
   ```bash
   these are not implemented in the project, venv is required with PCA9685 libraries
   pip install -r requirements.txt
   ```

## Usage

**Raspberry Pi:**
```bash
## Start video stream:
cd v4l2rtspserver
./v4l2rtspserver -W 640 -H 480 -F 30 -P 8554 -v -b 1000000 /dev/video0
## Run python script to start controlling the robot
cd ~/RaspberryPiScripts
python right_hand_servo_control_interruptable.py
```
## Computer side
Run OBStudio and recive RTP camera stream from Raspberry pi ip/port

**Unity:**
1. Open project in Unity
2. Press Play or build to VR headset

## Configuration

Make sure both devices are on the same network and update IP addresses in the connection scripts.

## Contact

Jovan Nikolov - https://github.com/JovanNikolov/VRobot
