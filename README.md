Bizagi Environment Synchronizer (BizagiEnvSync)
===============================================

What?
-----
This is a CLI replacement of Bizagi Advanced Deployment tool (BADT) which is GUI. It has three main functions:

1. EXPORT: Generate export package (BEX). Equivalent of Export.exe of BADT.
2. IMPORT: Analyze BEX and generate SQL diff scripts that can be applied to destination environment. Equivalent
   of CreateImport.exe of BADT.
3. CLEANUP: Remove all files that are older than 10 days in given dir. Dangerous - use with caution.

Both EXPORT and IMPORT run in configuration that is analogous to leaving GUI tool settings in default settings and
having checked exporting all processes.

Why?
----
Since the BEX generation+import in GUI takes pretty long, is a 2-step activity and distracts from other work, 
we wanted automated/scheduled generation of export/import artifacts, so they are ready at user's will.

Secondly, you can run one instance of BADT with multiple datasources, such as DEV->TEST + TEST->PRODUCTION,
which is not possible with BADT.

Building
--------
In order to build this code correctly, one must add references to BADT DLLs, i.e. be in posession of BADT. In order 
to obtatin BADT you have to be a registered user of BizAgi BPM Suite.

Running
-------
Run BizAgiEnvSync.exe without argument to get full list of options. When passing config file, it has to 
the be in same configuration format as CreateImport.exe.config or Export.exe.config in BADT. All filesystem paths 
have to be writable by the user under which is the app run.

Limitations and known issues
----------------------------
- BizagiEnvSync analyses all processes, user cannot choose to exclude one
- Due to poor implementation of certain methods in BizAgi DLLs, you have to use forward slashes '/' in EXPORT mode.
- Failing of EXPORT and IMPORT actions due to metadata inconsistencies has not been implemented yet, exceptions 
  are to be expected, in such case they will be analogous to failings of GUI export (BAD too)
- This is a version intended for BizAgi BPM Suite 9.1.x, version 10.x support will follow.
- Generate BEX and SQL files are not 1:1 copies of GUI exports, due to minor differences such as linking mscorlib 4.0.0.0
  instead of 2.0.0.0, different ordering of XML attributes
- Generally, this is an alpha-stage code.

Liability waiver
----------------
Use this software solely on your risk. I am not to be held responsible for any damage done.

Michal Stankay 
<michal@stankay.net>
