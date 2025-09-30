import time
from board import SCL, SDA
import busio
from adafruit_pca9685 import PCA9685
import socket
import signal
import sys

# Connection
UDP_IP = '0.0.0.0'
UDP_PORT = 6000

sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
sock.bind((UDP_IP, UDP_PORT))

# Servo control
i2c = busio.I2C(SCL, SDA)
pca = PCA9685(i2c)
pca.frequency = 50

servo_min = 500
servo_max = 2500

def angle_to_pwm(angle):
    pulse = servo_min + (servo_max - servo_min) * (angle / 180.0)
    duty_cycle = int(pulse * 65536 / 20000)
    return duty_cycle

def stop_servos():
    # Stop the servos by setting their duty cycle to 0 or neutral (e.g., middle of range)
#   for i in range(6):  # Assuming you have 6 servos
#        pca.channels[i].duty_cycle = 0  # Stop the servos
    pca.channels[0].duty_cycle = angle_to_pwm(90)
    pca.channels[1].duty_cycle = angle_to_pwm(180)
    pca.channels[2].duty_cycle = angle_to_pwm(90)
    pca.channels[3].duty_cycle = angle_to_pwm(90)
    pca.channels[4].duty_cycle = angle_to_pwm(90)
    pca.channels[5].duty_cycle = angle_to_pwm(150)
    pca.channels[6].duty_cycle = angle_to_pwm(90)
    pca.channels[12].duty_cycle = angle_to_pwm(90)
    pca.channels[13].duty_cycle = angle_to_pwm(120)
    
    pca.deinit()  # Deinitialize the PCA9685
    time.sleep(1.5)

# Graceful exit using signal handling
def signal_handler(sig, frame):
    print("Stopping servos and exiting...")
    stop_servos()
    sys.exit(0)

# Catch SIGINT (Ctrl+C) to handle the graceful shutdown
signal.signal(signal.SIGINT, signal_handler)

try:
    while True:
        try:
            data, addr = sock.recvfrom(1024)
        except socket.timeout:
            continue
        angles = data.decode().strip().split(',')
        yaw, pitch, roll, elbow, wrist, grip, headx, heady = map(float, angles)
        #print(pca.channels[0].duty_cycle)
        # Set the duty cycle for each channel
        pca.channels[2].duty_cycle = angle_to_pwm(yaw)
        pca.channels[1].duty_cycle = angle_to_pwm(pitch)
        pca.channels[0].duty_cycle = angle_to_pwm(roll)
        pca.channels[3].duty_cycle = angle_to_pwm(elbow)
        pca.channels[4].duty_cycle = angle_to_pwm(wrist)
        pca.channels[5].duty_cycle = angle_to_pwm(grip)
        pca.channels[12].duty_cycle = angle_to_pwm(headx)
        pca.channels[13].duty_cycle = angle_to_pwm(heady)

except KeyboardInterrupt:
    print("Interrupt received, stopping servos...")
    stop_servos()
    pca.deinit()

