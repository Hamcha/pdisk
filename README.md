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