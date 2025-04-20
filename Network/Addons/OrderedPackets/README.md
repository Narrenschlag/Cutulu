# Ordered Packets
When you use the UDP protocol you might miss the ordered stream of packets know from TCP. This addon helps you to achieve exactly that.

# Usage
To utilize the [OrderedPacket](./OrderedPacket.cs) class you need to create a new instance of the [PacketOrganizer](./PacketOrganizer.cs) class.
When done you can pack and unpack packets using the `PacketOrganizer.Pack` and `PacketOrganizer.Unpack` functions.

# Example
```csharp
// Create a new instance of the PacketOrganizer
var _packet_organizer = new PacketOrganizer();

// Pack a packet
var _packet = _packet_organizer.Pack(0, "Hello sir.");

// Unpack a packet
var _message = _packet_organizer.Unpack(0, _packet);
```

Note that the _key is used for the timestamping only and does not affect the actual packet data.