# Writing tests

- Every test must be able to fail if the behavior it covers breaks. Don't write tests that only check that a constructor runs, a value isn't null, or a mock returns what it was set up to return.
- Write expected values as hand-picked literals. Never compute them from the game's own constants, data, or logic; that checks the code against itself. Inside tests, the rules on named values and derived values don't apply, and repetition between tests is fine.
- Worked examples in the design docs make good test cases, such as the damage example in `combat.md` ((100 - 10) x 0.7 = 63).
- Test through the public API. Never make a member public, add an accessor, or use reflection just so a test can reach something. If logic can't be tested as written, say so and suggest a change rather than making it.
- One test per distinct behavior. When several inputs matter for the same behavior, use one parameterized test (`[TestCase]`).
- Follow the project's test naming and location convention. If there is none, tests for `Foo` go in a `FooTests` class under `Assets/_Project/Tests/`.
- Prefer EditMode tests. Use PlayMode tests only for behavior that needs frames, physics, or scene loading.
- `Awake`, `Start`, and other Unity messages don't run in EditMode tests; don't invoke them through reflection. Logic is easiest to test in plain C# classes wrapped by a thin MonoBehaviour. Suggest that structure rather than restructuring existing code unasked.
