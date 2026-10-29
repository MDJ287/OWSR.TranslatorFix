# OWSR.TranslatorFix

**(NOT YET SPEEDRUN-LEGAL)** patch for Outer Wilds making text translation no longer framerate dependent.

# Q&A

## Why not go through the Mod Loader?

The Outer Wilds Mod Loader has some effects that affect speedruns; for example, it adds an extra button in the pause menu, changing the behavior of quick-savequits. This patch only fixes the translator and does nothing else.

## How exactly does this patch work?

TODO

Temporary short answer: it uses [Unity Doorstop](https://github.com/NeighTools/UnityDoorstop) and [Mono.Cecil](https://www.mono-project.com/docs/tools+libraries/libraries/Mono.Cecil/) to modify the assembly of the game so that the translator translates more than 1 word per frame.

## Is this patch legal for speedruns?

Not yet! We will be holding a community vote at some point, but currently people seem to be in favor.

## I have a question/suggestion, or I think you made a mistake, or I want to discuss the patch

Please ping me (**@mdj287**) on the [Outer Wilds Speedrun Discord](https://discord.gg/4us2uATJpn)! I will be happy to respond to anything about the patch
