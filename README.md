# Tag Post Processor

This script generates a `TagConstants.cs` file containing constants for layers, scenes, tags, and more. The idea is to prevent runtime issues when you rename scenes or layers by using these constants in your code instead of methods like `LayerMask.NameToLayer("Default")`. The tradeoff is you will have compile-time errors that should be easier to find/fix.
