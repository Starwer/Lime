# Light Integrated Multimedia Environment (Lime)
Light Integrated Multimedia Environment (in short: **Lime**) is project aiming at providing a suite of Software enhancing the multimedia experience in Windows.
The project has the following ambitions:
- Provide a multimedia launcher which is easy to the hand and to the eyes, customizable, following the O.S. and multimedia standards to seamlessly integrate to the system
- Instantly adopts your multimedia collection as it is, without the need to reorganize it to a strict directory-structure (Like most Media-Centers do), change the video/audio/picture format (like some proprietary systems require) or opening a dedicated database for it (Like most Multimedia servers do).
- Provide full support of metadata, retrieving information about your movie and music on the Internet databases, but also enabling to customize these data for you own collection.
- Enhance the support of multimedia in the Window explorer itself: better thumbnails, better tag support, better view-pane
- Enable the control of your multimedia collection by different interface, with an equally good support of mouse, keyboard, touchscreens, touch-pads, remote-controls, but also network and DLNA access. 
 - This must be light in resources
- This must be free (open-source)
- This must be user-centric

## What's in the name

### Light
It just provides what’s missing to your system: an excellent support for multimedia. It doesn’t add things that are probably already there or where other Software do better, like a video/music player, codecs, pictures... 
It is cheap in disk-space, cheap in memory, cheap in compute power.

### Integrated
The aim is to blend Lime into your system, enhancing the support of multimedia in Windows. When Windows XP came, it introduced the thumbnail view, basic tag support for wmv, and a basic side-pane. I thought it was a matter of years before we would get magnificent movie/music file overview in Explorer, with cover displayed, description or plot, actors or musicians, rating… It somehow came in for the audio work. Still, 4 versions of Windows later (and many years), the videos look all equally dull, showing no info, hard to tag (if tagging is even possible for some formats), displaying a useless logo (first frames of the movie). I thought it was high time to improve this integration in Windows Explorer.
The Launcher gives you the features of a media-center… but using the tagging standards, the already installed tools, and the system features. Media-centers usually impose you to structure your collection into pre-defined directory structure, spread multiple files to handle “metadata”, and impose you to launch and render the content inside the media-center if you want to leverage the features they offer. Browsing your collection with the explorer after that conversion is even less enjoyable than before, as you need to browse through the extra hierarchy and find the video in the middle of the tens added files generated by the media-center. To make it work, you must then decide to either convert your system into a media center, or to use it as a normal computer.
I wanted a system that would allow to nicely browse and organize your collection in Windows Explorer, launch a video from there with your favorite player, continue watching this video full screen from your coach, and still being able to take control over it from there (using remote-control).

### Multimedia
It is about improving support and handling of Movie, Audio, and pictures. It is about controlling your Software with a remote control, while you sit on your coach. It’s about getting your content reachable/controllable from other devices using the standard formats and protocols.

### Environment
Lime is not an Application, it is a Software Suite which provides a complementary ecosystem of tools to handle the multimedia usage better.

## The projects
Lime is a suite of tools to handle different aspect of the multimedia requirement in a system. These are the different projects that are foreseen for Lime:

### Lime Launcher
This aims at being the main visible interface to the Lime suite. This is a Software which can optionally be put in your system tray, to browse, launch and edit your files.

- Provide a slick file launcher/browser, designed to optimally be used with mouse, keyboard, remote-control and touch-screen/pad.
- Skin-able, could be used as a system-style windowed application, or as a full-screen media-center-style browser 
- Main configuration interface for the Lime Suite
- Provide the mean to add, edit, and retrieve automatically metadata for your multimedia files. Metadata are for example cover, plot, actors, rating, description, comments… Metadata are added as tag **inside** the file. No external database or added nfo files. One file = One self-contained video and its metadata.
- This use the normal Windows directory structure (Integrated) and shell links to organize your files. You can also browse the same structure in Windows Explorer and drag/drop copy/paste both ways.

### Thumb Lime
This is a Windows shell-extension which will enable Windows to better support the multimedia files. This allows you to:
- Display covers instead of first frames in Explorer Thumbnails
- Access metadata directly from the Windows Explorer
- Display relevant information and cover on the Windows Explorer View Pane

### More to come

- Multi-screen automations
- Support of DLNA standard to access/control you multimedia collection

