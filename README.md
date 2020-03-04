# ExtendedListView
An extension of the exisiting WinForms ListView .NET control to allow reordering of the list using intuitive drag and drop methods.

##################

This repository includes the extended ListView control class and an example Form class to demonstrate how to utilise
the control using the appropriate event handlers.

Drawing of the line to signify the item's drop position is performed by overriding the WndProc() method as the ListView doesn't utilise
the OnPaint() method that is normally referred to when drawing on WinForms controls.

##################

Note: The colour of the drag and drop position line can be changed by editing the Color passed to the two variables named:
 - pearBlueBrush
 - pearBluePen
 
 These are used when colouring the polygons at either end of the line and the line itself respectively.
 
Note: This code can not be directly copied and pasted into your solution, instead you will need to copy only constituient parts
and adapt solution specific code to match your own solution as I wrote this as part of an exisiting solution that required a 
ListView such as this.


##################

Happy Coding,
Liam
