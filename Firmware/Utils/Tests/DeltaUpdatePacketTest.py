import serial
import time

PORT = "COM5"
BAUD = 115200

ser = serial.Serial(PORT, BAUD, timeout=1)
time.sleep(2)  # Let ESP32-S2 reset

# Step 1: Send HELLO_LUMIN as ASCII
ser.write(b"HELLO_LUMIN")
print(">>> Sent HELLO_LUMIN")

# Step 2: Wait for ACK
start = time.time()
while time.time() - start < 5:
    if ser.in_waiting:
        print(ser.read(ser.in_waiting).decode(errors="ignore"), end="")
    time.sleep(0.1)

# Step 3: Construct DeltaUpdatePacket
name = "Innocn 34E7R".encode("ascii")
name_padded = name + b"\x00" * (32 - len(name))
packet = bytes([
    0xAA,       # Start byte
    0x24,       # Length = 36 bytes
    0x02,       # PacketType = DeltaUpdate
]) + name_padded + bytes([
    0x01,       # id
    0x28,       # value = 40
    0x01        # deviceType = Brightness
])

# Step 4: Send packet
ser.write(packet)
print("\n>>> Sent DeltaUpdatePacket")

# Step 5: Wait for response
print("\n<<< Response:")
start = time.time()
while time.time() - start < 5:
    if ser.in_waiting:
        print(ser.read(ser.in_waiting).decode(errors="ignore"), end="")
    time.sleep(0.1)

ser.close()
