# The Cutulu SDK
Inherting its name from the lovecraftian being Cthulhu, this SDK serves as foundation for software projects powered by godot.

# Where to Start
The `Core.cs` file provides you with the essential utility known from other engines like unity. Fast debugging processes, static raycasting, simple mod support and save file managment, just to name a few of the utility tools provided in this SDK.

# Why OpenSource
Sole purpose of this SDK is to improve overall software products and their production cycles by providing optimized and efficient code foundations to almost tailored extend without needing to learn complex algorithms, writing far too many lines of code or producing flaws in the code due to forgetting details.

# History of Cutulu
Formerly called NarrenAPI or Raven the Narrenschlag SDK has gone through a lot of iterations. After switching from Unity to godot and participating in the Franken Game Jam Bayreuth in Nov, 2023, a co jammer planted the seed of an open source library in my head. Since I am a big fan of Linux and GNU aswell as Godot and OpenSource in general I decided to publish it. As the SDK evolved with my personal experience it stands as my state of the art essence of what I personally think is needed to make high quality software development in no time a reality. Because a great artist can only become an icon if he has the right tools.

# Author
My name is Maximilian Schecklmann, also known as Narrenschlag or MaxNar and founder of Software Narrenschlag. My passion and profession is creating software, solving problems, creating art and enhancing the everyday experience of people. My coding journey started in game development and it's this heritage that I base all my interest and experience on till this day. As my personal ambition is to become the **Rodin** of my generation, I want to build my legacy by envoking emotion in people through art and high quality. By supporting this SDK you support my dream and as you support me you simultaneously support this project. Therefore. Thank you.

~ *Max*



# Networking
Comming with Cutulu and inheriting from Walhalla network support is easy to use and efficient. For the server side you override (Server: ServerNetwork, ServerClient and Destination) and for the client side you override (ClientNetwork, Destination). Nagle's Algorithm is handled by the TcpProtocol class. A sample (Unit Test) is included in the repository.
