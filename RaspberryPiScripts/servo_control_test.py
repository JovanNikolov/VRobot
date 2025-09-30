import time
from board import SCL, SDA
import busio
from adafruit_pca9685 import PCA9685

i2c = busio.I2C(SCL, SDA)

pca = PCA9685(i2c)
pca.frequency = 50

servo_min = 500
servo_max = 2500

def angle_to_pwm(angle):
    pulse = servo_min + (servo_max - servo_min) * (angle / 180.0)
    duty_cycle = int(pulse * 65536 / 20000)
    return duty_cycle

while(True):
    time.sleep(5)
    pca.channels[0].duty_cycle = angle_to_pwm(10)
    #time.sleep(1.5)
    #pca.channels[0].duty_cycle = angle_to_pwm(30)
    #time.sleep(0.5)
    #pca.channels[0].duty_cycle = angle_to_pwm(60)
    #time.sleep(0.5)
    #pca.channels[0].duty_cycle = angle_to_pwm(90)
    #time.sleep(0.5)
    #pca.channels[0].duty_cycle = angle_to_pwm(120)
    #time.sleep(0.5)
    #pca.channels[0].duty_cycle = angle_to_pwm(150)
    #time.sleep(0.5)
    #pca.channels[0].duty_cycle = angle_to_pwm(180)
    #time.sleep(1.5)
    #pca.channels[0].duty_cycle = angle_to_pwm(90)
    #time.sleep(1.5)
#pca.channels[5].duty_cycle = angle_to_pwm(120)
#time.sleep(1.5)
#pca.channels[5].duty_cycle = angle_to_pwm(171)
#time.sleep(1.5)
#for angle in range(0, 181, 10):
#    pca.channels[0].duty_cycle = angle_to_pwm(angle)
#    pca.channels[1].duty_cycle = angle_to_pwm(angle)
#    pca.channels[2].duty_cycle = angle_to_pwm(angle)
#    pca.channels[3].duty_cycle = angle_to_pwm(angle)
#    pca.channels[4].duty_cycle = angle_to_pwm(angle)
#    pca.channels[5].duty_cycle = angle_to_pwm(angle)

#    time.sleep(0.1)


pca.deinit()
