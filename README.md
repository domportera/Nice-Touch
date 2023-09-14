# Nice-Touch
A Godot library for seamless multitouch interaction in C#

Hello to any onlookers - Android export has since been merged into a Godot minor release dev build. Currently there is no documentation for this package and minimal integrations with other projects, and this has not been properly modified to adapt to Godot 4. I expect the latter to be simple enough, however I'm certain in the development process I will not been aware of some use cases. If any use case seems uncovered, I encourage you to make a Discussion post.

Currently, the model is as follows: this package intercepts native Godot touch events and forms its own single and multi touch gestures. It supports multiple simultaneous gestures and passes them along to nodes prepared to receive it.

Nodes "subscribe to" new gestures and receive updates relevant to that gesture. A simple example use case could be flingable objects - a node would receive a gesture and can follow that gesture through to its release with little to no logic required - just an interface implementation.

Imagine having dozens of objects on screen where each finger can grab an object to throw it. Or imagine having a canvas with multiple images, where each can be individually resized and rotated via pinch gestures with a theoretically unlimited number of hands interacting with it.

Multi-touch UI is also something I've managed to get working - a longstanding pain point for godot in my limited experience. I developed this for a remote keyboard application, the fate of which is currently undecided lol

Currently this is C# only - not sure how this should interoperate with GDScript at this point. Ideas are welcome on this front, as I have near zero experience with GDScript and no awareness of best practices for it.
