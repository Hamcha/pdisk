
Persistence
===========

Persistence is a VFS (Virtual File System) that tries to tie Ramdisks to persistent local storage.

The concept
-----------

Ramdisks are extremely fast, they have two disadvantages though:

- It's temporary storage, as soon as the ram gets flushed (ex: when powering off) it's all gone
- It's limited by the available RAM, which is usually very limited when compared to local storage.

The solution I wanted to try was to have a Ramdisk-like VFS with files packed into big chunks that get loaded from permanent storage, modified directly in memory and then saved back to storage.

The bottleneck is the loading of chunks, which could be optimized by choosing the right chunk size.

The implementation
------------------

Pdisk (short for Persistence) is being developed in C# (.net 4.0) using the [Dokan User-mode FS driver](http://dokan-dev.net/en/) which is kinda like [FUSE](http://fuse.sourceforge.net/) but for Windows.

Files are saved in two places:  
The content is saved into chunks (.chunk) files in a binary packing fashion.  
The file information and other metadata is stored into metadata files (.meta) using JSON as a format (and [JSON.net](http://james.newtonking.com/projects/json-net.aspx) as a library for handling them)

What works now
--------------

Settings are read from a JSON "settings.conf" file

You can create, edit, delete files and directories, and it *should* work fine, if you find any bugs please let me know.

The chunks and their metadata are saved in a Redis-fashion using edit/intervals in which everything is saved if more than **X** edits have been done in **Y** seconds (you can set more than one interval)

What doesn't work (yet)
-----------------------

Setting files permissions as well as locking/unlocking files.
 
They are not priorities so I don't think I'm going to make them work any time soon

Things I'm doing (a.k.a To-do list)
----------------

Compression of chunk and metadata using [Snappy](https://code.google.com/p/snappy/) ([.NET binding](http://snappy4net.codeplex.com/))

Unloading of unused chunks