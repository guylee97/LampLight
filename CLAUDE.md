# LampLight

Top-down 2D stealth game. Unity 6000.3.20f1, URP 2D.

## Engineering principles

- Before designing a solution, look at how established products solve the same problem. Adopt proven patterns and conventions instead of inventing an approach from scratch.
- Do not preserve backward compatibility. Delete unused paths rather than bolting on compatibility layers, fallbacks, or migrations.
- Choose the simplest implementation that fully satisfies the current requirements. Do not build speculative abstractions, config knobs, or indirection layers.
- Grow systems in layers. Start from a minimal version that works end to end, then add one feature at a time on top of something that already works. Never trade working code for unfinished complexity.
- Split components into modules with clear separation of concerns.
- Use a proven, maintained library when it lowers overall complexity or improves stability. Do not reimplement common functionality without a clear reason.
- Check the dependencies already installed before implementing something yourself or adding a package. Never assert that "this library can't do that" without checking its docs and types.
- Make architectural decisions for the long term. Do not accept stopgaps that only get you through today and will be replaced later.
