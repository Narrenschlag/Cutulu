# Binary Encoding
Welcome to my little nook of magic. Hidden in these woods called GitHub you will find something that is making me ecstatic everytime I think about it!

You can basically en/decode everything. If you want to optimze or take more control you can write you own BinaryEncoder<T>. You don't have to register or place it anywhere. Simply inherit from BinaryEncoder<YourType>. By overwriting the Priority value to use it over any other registered Encoder.

### Roadmap
Just added generic Dictionary<,> en/decoding support. Next I plan to add support for KeyValuePair<,> aswell. Right now I am also thinking about adding List<> support aswell even though that has less priority. Stay tuned. Highly performant as always haha.

### Careful with the following
- **Only encodes public T ValueName { get; set; }**: Value has to be public and have a public get and set accessors.
- **Decoding plain text from bytes**: When loading plain text as byte[] from files you have to use StreamReader. Take a look on how I do it in [File.cs](./Core/FileSystem/File.cs)!
- **List<> is not supported**: You can solve this by using arrays instead.
- **KeyValuePair<,> is not supported**: Use a custom struct instead.
- **Tuples (for example (T0 a, T1 b, T2)) are not supported**: Use a custom struct instead.