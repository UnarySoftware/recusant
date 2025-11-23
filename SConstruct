#!/usr/bin/env python
import os
import sys
import shutil
import SCons
from pathlib import Path

def GlobRecursive(pattern, node='.'):
    results = []
    for f in Glob(str(node) + '/*', source=True):
        if type(f) is SCons.Node.FS.Dir:
            results += GlobRecursive(pattern, f)
    results += Glob(str(node) + '/' + pattern, source=True)
    return results

def ensure_file(source, dest):
    if not os.path.isfile(os.path.abspath(dest)):
        shutil.copyfile(os.path.abspath(source), os.path.abspath(dest))
    return

env = SConscript("../godot-cpp/SConstruct")

if env.get("is_msvc", False):
    env.Prepend(CXXFLAGS=["/std:c++20"])
else:
    env.Prepend(CXXFLAGS=["-std=c++20"])

env.Append(CPPPATH=["sources/"])
env.Append(CPPPATH=["../steam/include"])
env.Append(LIBPATH=["../steam"])
if env["platform"] == "windows":
    env.Append(LIBS=["steam_api64"])
else:
    env.Append(LIBS=["steam_api"])

sources = GlobRecursive("*.cpp", "sources")

if env["target"] in ["editor", "template_debug"]:
    try:
        doc_data = env.GodotCPPDocData("sources/gen/doc_data.gen.cpp", source=Glob("doc_classes/*.xml"))
        sources.append(doc_data)
    except AttributeError:
        print("Not including class reference as we're targeting a pre-4.3 baseline.")

# Remove default O2 flag
if '-O2' in env['CCFLAGS']:
    env['CCFLAGS'].remove('-O2')

if env["target"] == "editor" or env["target"] == "template_debug":
    env.Append(CCFLAGS='-O0 -g -fno-inline -Wall -Wextra -Wimplicit -Werror -Wno-unused-parameter')
    env.Append(CPPDEFINES=["DISABLE_DEPRECATED", "DEBUG_ENABLED"])
else:
    env.Append(CCFLAGS='-O3 -Wall -Wextra -Wimplicit -Werror -Wno-unused-parameter')
    env.Append(CPPDEFINES=["DISABLE_DEPRECATED"])

library = env.SharedLibrary(
    "bin/game{}{}".format(env["suffix"], env["SHLIBSUFFIX"]),
    source=sources,
)

Default(library)

Path("bin").mkdir(parents=True, exist_ok=True)

if env["platform"] == "windows":
    ensure_file("../windows/mingw64/bin/libgcc_s_seh-1.dll", "bin/libgcc_s_seh-1.dll")
    ensure_file("../windows/mingw64/bin/libstdc++-6.dll", "bin/libstdc++-6.dll")
    ensure_file("../windows/mingw64/bin/libwinpthread-1.dll", "bin/libwinpthread-1.dll")
    ensure_file("../steam/steam_api64.dll", "bin/steam_api64.dll")
    ensure_file("steam_appid.txt", "bin/steam_appid.txt")
elif env["platform"] == "linux":
    ensure_file("../steam/libsteam_api.so", "bin/libsteam_api.so")
    ensure_file("steam_appid.txt", "bin/steam_appid.txt")
