Don't bother touching line endings and BOMs.
Let tidy_code handle that as needed.

When I say "build" or "test" run `./build-and-test.cmd`.
Also, automatically do this before each commit.

## Secrets

Always obscure API keys and other secrets, especially in saved screenshots.

## C#

A `switch` should only ever have a single statement in a case.
If it's going to be more than that, extract.

Avoid ternary (`?:`) except for very simple cases.

Don't use `sealed` on classes or records.

The most important thing about a name is that it not be misleading.
A reader should never need to look at how a variable or method is used/implemented to understand what it represents.
It is OK to make the name long to achieve this.
If you see a comment explaining what a function or variable does/is, rename it to make it more obvious.

## Committing

When I say "commit", first run `./build-and-test.cmd`.
If the change has more than one thing going on, split it into multiple commits, each with a clear purpose.
Especially separate refactoring from behavior changes.
