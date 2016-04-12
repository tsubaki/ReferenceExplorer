ReferenceExplorer
=================



![work refrence explorer](https://github.com/tsubaki/ReferenceExplorer/blob/gh-pages/images/re5.gif?raw=true)

##Visualize the relationships of objects/components

When a GameObject is selected red and blue lines will appear showing, respectively, the objects to which it refers and the objects that have references to it.

"References" are any properties, variables or events (both C# Events and UnityEvents) that refer to another object.

![reference viewer](https://github.com/tsubaki/ReferenceExplorer/blob/gh-pages/images/ref1.png)

##Visualize the objects that will receive function calls

- The "Callback" window: visualize the objects that will receive function calls

Callback methods (that is, SendMessage/AnimationEvent/Collison/etc methods) are listed in this window, and show the objects that can receive/send these events.

![callback1](https://github.com/tsubaki/ReferenceExplorer/blob/gh-pages/images/callback11.jpg?raw=true)
![callback2](https://github.com/tsubaki/ReferenceExplorer/blob/gh-pages/images/message2.jpg?raw=true)

##Lists Components in current scene or Selected Objects

This window displays the objects in the scene or in the selection, the relationship between the components.

![lists](https://github.com/tsubaki/ReferenceExplorer/blob/gh-pages/images/count.png?raw=true)

##Search Codes in scenes

By searching for the name of a callback, you can quickly track down which objects are responsible for certain functions, like finding the Component that is responsible for deciding if the game is over:

![search](https://github.com/tsubaki/ReferenceExplorer/blob/gh-pages/images/search.gif?raw=true)

##Create Relationship Graph of Object/class

This makes it possible to visualize, outside of Unity, the entire architecture of a scene's GameObjects and components.
 
this feature requests [yEd](https://www.yworks.com/products/yed)


![graph](https://github.com/tsubaki/ReferenceExplorer/blob/gh-pages/images/graph1.png?raw=true)
