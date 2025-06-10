# Binary Encoding
Welcome to my little nook of magic. Hidden in these woods called GitHub you will find something that is making me ecstatic everytime I think about it!

You can basically en/decode everything. If you want to optimze or take more control you can write you own BinaryEncoder<T>. You don't have to register or place it anywhere. Simply inherit from BinaryEncoder<YourType>. By overwriting the Priority value to use it over any other registered Encoder. There are only a few complex class structures left that are not encodable. But I am comming for them! ;D

### Roadmap
Just added generic support for KeyValuePair<>, Dictionary<,>, Tuples and List<>. Stay tuned and highly performant as always haha.

### Careful with the following
- **Only encodes public T ValueName { get; set; } is classes and structs**: Value has to be public and have a public get and set accessors.
- **Decoding plain text from bytes**: When loading plain text as byte[] from files you have to use StreamReader. Take a look on how I do it in [File.cs](./Core/FileSystem/File.cs)!