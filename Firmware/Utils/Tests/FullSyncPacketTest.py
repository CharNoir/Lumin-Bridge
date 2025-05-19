import serial
import time

PORT = "COM6"
BAUD = 115200

ser = serial.Serial(PORT, BAUD, timeout=1)
time.sleep(2)

# Step 1: Send HELLO_LUMIN
ser.write(b"HELLO_LUMIN")
print(">>> Sent HELLO_LUMIN")

# Step 2: Wait for ACK
start = time.time()
while time.time() - start < 5:
    if ser.in_waiting:
        print(ser.read(ser.in_waiting).decode(errors="ignore"), end="")
    time.sleep(0.1)

# Step 3: Build FullSyncPacket with 3 devices
devices = []

def create_device(name_str, id, value, device_type):
    name_bytes = name_str.encode("ascii")
    name_padded = name_bytes + b"\x00" * (32 - len(name_bytes))
    return name_padded + bytes([id, value, device_type])

# Device 1 – Volume
devices.append(create_device("Speaker 1", 1, 60, 0))  # Volume = type 0

# Device 2 – Volume
devices.append(create_device("Speaker 2", 2, 70, 0))  # Volume = type 0

# Device 3 – Brightness
devices.append(create_device("Innocn 34E7R", 3, 40, 1))  # Brightness = type 1

# Combine devices into one byte array
device_payload = b''.join(devices)

# Payload = [PacketType][Count][Devices...]
payload = bytes([
    0x01,       # PacketType::FullSync
    len(devices)  # count = 3
]) + device_payload

packet = bytes([
    0xAA,          # Start byte
    len(payload)   # Length byte = payload size
]) + payload

# Step 4: Send the packet
ser.write(packet)
print("\n>>> Sent FullSyncPacket with 3 devices")

# Step 5: Read response
print("\n<<< Response:")
start = time.time()
while time.time() - start < 5:
    if ser.in_waiting:
        print(ser.read(ser.in_waiting).decode(errors="ignore"), end="")
    time.sleep(0.1)

ser.close()
