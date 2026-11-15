using AwesomeAssertions;
using Microsoft.Playwright;

namespace Lantean.QBTMud.Test.JavaScript
{
    public sealed class KeyboardInteropTests
    {
        private const string _callbackInitialization = """
            window.calls = [];
            window.keydowns = [];
            window.createKeyboardOwner = (id, name) => ({
                _id: id,
                invokeMethodAsync: (method, args) => {
                    window.calls.push({ owner: name, method, ...args });
                    return Promise.resolve();
                }
            });
            window.background = window.createKeyboardOwner("Background", "Background");
            window.wizard = window.createKeyboardOwner("Wizard", "Wizard");
            window.preview = window.createKeyboardOwner("Preview", "Preview");
            """;

        [Theory]
        [InlineData("Enter", false)]
        [InlineData("Enter", true)]
        [InlineData(" ", false)]
        [InlineData(" ", true)]
        public async Task GIVEN_RegisteredTableShortcut_WHEN_NativeButtonActivated_THEN_ShouldClickWithoutDispatchingShortcut(string key, bool focusedOwner)
        {
            await using var target = await JavaScriptTestPage.CreateAsync("interop.js", _callbackInitialization);
            await target.Page.EvaluateAsync("""
                args => {
                    qbt.registerKeypressEvent({ key: args.key }, window.background);
                    if (args.focusedOwner) {
                        qbt.keyPressFocusInstance(window.background);
                    }
                    const button = document.getElementById("native-action");
                    button.setAttribute("aria-expanded", "false");
                    button.addEventListener("click", () => button.setAttribute("aria-expanded", "true"));
                    document.addEventListener("keydown", event => window.keydowns.push(event.defaultPrevented));
                }
                """, new { key, focusedOwner });
            var button = target.Page.GetByRole(AriaRole.Button, new() { Name = "Native action" });
            await button.FocusAsync();

            await target.Page.Keyboard.PressAsync(key == " " ? "Space" : key);

            (await button.GetAttributeAsync("aria-expanded")).Should().Be("true");
            (await target.Page.EvaluateAsync<string[]>("() => window.calls.map(call => call.owner)")).Should().BeEmpty();
            (await target.Page.EvaluateAsync<bool[]>("() => window.keydowns")).Should().Equal(false);
        }

        [Theory]
        [InlineData("Control", "ctrlKey")]
        [InlineData("Shift", "shiftKey")]
        [InlineData("Alt", "altKey")]
        [InlineData("Meta", "metaKey")]
        public async Task GIVEN_NativeButton_WHEN_ModifiedEnterPressed_THEN_ShouldDispatchExplicitShortcut(string modifier, string property)
        {
            await using var target = await JavaScriptTestPage.CreateAsync("interop.js", _callbackInitialization);
            await target.Page.EvaluateAsync("args => qbt.registerKeypressEvent({ key: 'Enter', [args]: true }, window.background)", property);
            await target.Page.GetByRole(AriaRole.Button, new() { Name = "Native action" }).FocusAsync();

            await target.Page.Keyboard.PressAsync(modifier + "+Enter");

            (await target.Page.EvaluateAsync<string[]>("() => window.calls.map(call => call.owner)")).Should().Equal("Background");
        }

        [Fact]
        public async Task GIVEN_NestedKeyboardOwners_WHEN_PreviewCloses_THEN_ShouldRestoreWizardBeforeBackground()
        {
            await using var target = await JavaScriptTestPage.CreateAsync("interop.js", _callbackInitialization);
            await target.Page.EvaluateAsync("""
                () => {
                    document.addEventListener("keydown", event => window.keydowns.push(event.defaultPrevented));
                    qbt.registerKeypressEvent({ key: "Enter" }, window.background);
                    qbt.registerKeypressEvent({ key: "Enter" }, window.wizard);
                    qbt.registerKeypressEvent({ key: "Enter" }, window.preview);
                    qbt.keyPressFocusInstance(window.wizard);
                }
                """);
            await target.Page.GetByRole(AriaRole.Button, new() { Name = "Keyboard target" }).FocusAsync();

            await target.Page.Keyboard.PressAsync("Enter");
            await target.Page.EvaluateAsync("() => qbt.keyPressFocusInstance(window.preview)");
            await target.Page.Keyboard.PressAsync("Enter");
            await target.Page.EvaluateAsync("""
                () => {
                    qbt.unregisterKeypressEvent({ key: "Enter" }, window.preview);
                    qbt.keyPressUnFocusInstance(window.preview);
                }
                """);
            await target.Page.Keyboard.PressAsync("Enter");
            await target.Page.EvaluateAsync("""
                () => {
                    qbt.unregisterKeypressEvent({ key: "Enter" }, window.wizard);
                    qbt.keyPressUnFocusInstance(window.wizard);
                }
                """);
            await target.Page.Keyboard.PressAsync("Enter");

            (await target.Page.EvaluateAsync<string[]>("() => window.calls.map(call => call.owner)"))
                .Should().Equal("Wizard", "Preview", "Wizard", "Background");
            (await target.Page.EvaluateAsync<bool[]>("() => window.keydowns"))
                .Should().Equal(true, true, true, true);
        }

        [Fact]
        public async Task GIVEN_WizardWithoutMatchingShortcut_WHEN_NestedPreviewCloses_THEN_ShouldKeepBackgroundSuppressedUntilWizardCloses()
        {
            await using var target = await JavaScriptTestPage.CreateAsync("interop.js", _callbackInitialization);
            await target.Page.EvaluateAsync("""
                () => {
                    document.addEventListener("keydown", event => window.keydowns.push(event.defaultPrevented));
                    qbt.registerKeypressEvent({ key: "Enter" }, window.background);
                    qbt.registerKeypressEvent({ key: "ArrowLeft" }, window.preview);
                    qbt.keyPressFocusInstance(window.wizard);
                }
                """);
            await target.Page.GetByRole(AriaRole.Button, new() { Name = "Keyboard target" }).FocusAsync();

            await target.Page.Keyboard.PressAsync("Enter");
            (await target.Page.EvaluateAsync<string[]>("() => window.calls.map(call => call.owner)")).Should().BeEmpty();
            await target.Page.EvaluateAsync("() => qbt.keyPressFocusInstance(window.preview)");
            await target.Page.Keyboard.PressAsync("ArrowLeft");
            await target.Page.EvaluateAsync("""
                () => {
                    qbt.unregisterKeypressEvent({ key: "ArrowLeft" }, window.preview);
                    qbt.keyPressUnFocusInstance(window.preview);
                }
                """);
            await target.Page.Keyboard.PressAsync("Enter");
            (await target.Page.EvaluateAsync<string[]>("() => window.calls.map(call => call.owner)")).Should().Equal("Preview");
            await target.Page.EvaluateAsync("() => qbt.keyPressUnFocusInstance(window.wizard)");
            await target.Page.Keyboard.PressAsync("Enter");

            (await target.Page.EvaluateAsync<string[]>("() => window.calls.map(call => call.owner)")).Should().Equal("Preview", "Background");
            (await target.Page.EvaluateAsync<bool[]>("() => window.keydowns")).Should().Equal(false, true, false, true);
        }

        [Fact]
        public async Task GIVEN_NestedKeyboardOwners_WHEN_LowerOwnerIsDisposedFirst_THEN_ShouldKeepTopOwnerThroughUnknownAndRepeatedUnfocus()
        {
            await using var target = await JavaScriptTestPage.CreateAsync("interop.js", _callbackInitialization);
            await target.Page.EvaluateAsync("""
                () => {
                    qbt.registerKeypressEvent({ key: "Enter" }, window.background);
                    qbt.registerKeypressEvent({ key: "Enter" }, window.wizard);
                    qbt.registerKeypressEvent({ key: "Enter" }, window.preview);
                    qbt.keyPressFocusInstance(window.wizard);
                    qbt.keyPressFocusInstance(window.preview);
                    qbt.unregisterKeypressEvent({ key: "Enter" }, window.wizard);
                    qbt.keyPressUnFocusInstance(window.wizard);
                }
                """);
            await target.Page.GetByRole(AriaRole.Button, new() { Name = "Keyboard target" }).FocusAsync();

            await target.Page.Keyboard.PressAsync("Enter");
            await target.Page.EvaluateAsync("""
                () => {
                    qbt.keyPressUnFocusInstance(window.wizard);
                    qbt.keyPressUnFocusInstance({ _id: "Unknown" });
                }
                """);
            await target.Page.Keyboard.PressAsync("Enter");
            await target.Page.EvaluateAsync("""
                () => {
                    qbt.unregisterKeypressEvent({ key: "Enter" }, window.preview);
                    qbt.keyPressUnFocusInstance(window.preview);
                }
                """);
            await target.Page.Keyboard.PressAsync("Enter");

            (await target.Page.EvaluateAsync<string[]>("() => window.calls.map(call => call.owner)"))
                .Should().Equal("Preview", "Preview", "Background");
        }

        [Fact]
        public async Task GIVEN_RepeatedFocusAndReactivatedWizard_WHEN_EachOwnerUnfocusesOnce_THEN_ShouldRestoreEachOwnerWithoutStaleDuplicates()
        {
            await using var target = await JavaScriptTestPage.CreateAsync("interop.js", _callbackInitialization);
            await target.Page.EvaluateAsync("""
                () => {
                    qbt.registerKeypressEvent({ key: "Enter" }, window.background);
                    qbt.registerKeypressEvent({ key: "Enter" }, window.wizard);
                    qbt.registerKeypressEvent({ key: "Enter" }, window.preview);
                    qbt.keyPressFocusInstance(window.wizard);
                    qbt.keyPressFocusInstance(window.wizard);
                    qbt.keyPressFocusInstance(window.preview);
                    qbt.keyPressFocusInstance(window.preview);
                    qbt.keyPressFocusInstance(window.wizard);
                }
                """);
            await target.Page.GetByRole(AriaRole.Button, new() { Name = "Keyboard target" }).FocusAsync();

            await target.Page.Keyboard.PressAsync("Enter");
            await target.Page.EvaluateAsync("""
                () => {
                    qbt.unregisterKeypressEvent({ key: "Enter" }, window.wizard);
                    qbt.keyPressUnFocusInstance(window.wizard);
                }
                """);
            await target.Page.Keyboard.PressAsync("Enter");
            await target.Page.EvaluateAsync("""
                () => {
                    qbt.unregisterKeypressEvent({ key: "Enter" }, window.preview);
                    qbt.keyPressUnFocusInstance(window.preview);
                }
                """);
            await target.Page.Keyboard.PressAsync("Enter");

            (await target.Page.EvaluateAsync<string[]>("() => window.calls.map(call => call.owner)"))
                .Should().Equal("Wizard", "Preview", "Background");
        }

        [Fact]
        public async Task GIVEN_NewReferenceWithExistingOwnerId_WHEN_RegistrationAndFocusRefresh_THEN_ShouldUseLatestCallbackAndRemoveOwnershipById()
        {
            await using var target = await JavaScriptTestPage.CreateAsync("interop.js", _callbackInitialization);
            await target.Page.EvaluateAsync("""
                () => {
                    qbt.registerKeypressEvent({ key: "Enter" }, window.background);
                    qbt.registerKeypressEvent({ key: "Enter" }, window.wizard);
                    qbt.keyPressFocusInstance(window.wizard);
                }
                """);
            await target.Page.GetByRole(AriaRole.Button, new() { Name = "Keyboard target" }).FocusAsync();

            await target.Page.Keyboard.PressAsync("Enter");
            await target.Page.EvaluateAsync("""
                () => {
                    window.replacement = window.createKeyboardOwner("Wizard", "Replacement");
                    qbt.registerKeypressEvent({ key: "Enter" }, window.replacement);
                    qbt.keyPressFocusInstance(window.replacement);
                }
                """);
            await target.Page.Keyboard.PressAsync("Enter");
            await target.Page.EvaluateAsync("""
                () => {
                    qbt.unregisterKeypressEvent({ key: "Enter" }, window.replacement);
                    qbt.keyPressUnFocusInstance(window.wizard);
                }
                """);
            await target.Page.Keyboard.PressAsync("Enter");

            (await target.Page.EvaluateAsync<string[]>("() => window.calls.map(call => call.owner)"))
                .Should().Equal("Wizard", "Replacement", "Background");
        }

        [Fact]
        public async Task GIVEN_EditableInput_WHEN_EnterAndControlEnterPressed_THEN_ShouldIgnorePlainEnterAndDispatchControlEnter()
        {
            await using var target = await JavaScriptTestPage.CreateAsync("interop.js", _callbackInitialization);
            await target.Page.EvaluateAsync("""
                () => {
                    document.addEventListener("keydown", event => {
                        if (event.key === "Enter") {
                            window.keydowns.push(event.defaultPrevented);
                        }
                    });
                    qbt.registerKeypressEvent({ key: "Enter" }, window.wizard);
                    qbt.registerKeypressEvent({ key: "Enter", ctrlKey: true }, window.wizard);
                    qbt.keyPressFocusInstance(window.wizard);
                }
                """);
            await target.Page.GetByRole(AriaRole.Textbox, new() { Name = "Editor" }).FocusAsync();

            await target.Page.Keyboard.PressAsync("Enter");
            (await target.Page.EvaluateAsync<string[]>("() => window.calls.map(call => call.owner)")).Should().BeEmpty();
            await target.Page.Keyboard.PressAsync("Control+Enter");

            (await target.Page.EvaluateAsync<string[]>("() => window.calls.map(call => call.owner)")).Should().Equal("Wizard");
            (await target.Page.EvaluateAsync<bool>("() => window.calls[0].ctrlKey")).Should().BeTrue();
            (await target.Page.EvaluateAsync<string>("() => window.calls[0].method")).Should().Be("HandleKeyPressEvent");
            (await target.Page.EvaluateAsync<bool[]>("() => window.keydowns")).Should().Equal(false, true);
        }

        [Fact]
        public async Task GIVEN_NoFocusedOwner_WHEN_RegisteredShortcutPressed_THEN_ShouldNotifyAllRegisteredOwners()
        {
            await using var target = await JavaScriptTestPage.CreateAsync("interop.js", _callbackInitialization);
            await target.Page.EvaluateAsync("""
                () => {
                    document.addEventListener("keydown", event => window.keydowns.push(event.defaultPrevented));
                    qbt.registerKeypressEvent({ key: "Enter" }, window.background);
                    qbt.registerKeypressEvent({ key: "Enter" }, window.wizard);
                    qbt.registerKeypressEvent({ key: "Enter" }, window.preview);
                }
                """);
            await target.Page.GetByRole(AriaRole.Button, new() { Name = "Keyboard target" }).FocusAsync();

            await target.Page.Keyboard.PressAsync("Enter");

            (await target.Page.EvaluateAsync<string[]>("() => window.calls.map(call => call.owner)"))
                .Should().Equal("Background", "Wizard", "Preview");
            (await target.Page.EvaluateAsync<bool[]>("() => window.keydowns")).Should().Equal(true);
        }

        [Fact]
        public async Task GIVEN_UnknownAndUnregisteredShortcuts_WHEN_KeysPressed_THEN_ShouldLeaveDefaultBehaviourAndSkipCallbacks()
        {
            await using var target = await JavaScriptTestPage.CreateAsync("interop.js", _callbackInitialization);
            await target.Page.EvaluateAsync("""
                () => {
                    document.addEventListener("keydown", event => window.keydowns.push(event.defaultPrevented));
                    qbt.unregisterKeypressEvent({ key: "Escape" }, window.wizard);
                    qbt.registerKeypressEvent({ key: "Enter" }, window.wizard);
                    qbt.unregisterKeypressEvent({ key: "Enter" }, window.wizard);
                }
                """);
            await target.Page.GetByRole(AriaRole.Button, new() { Name = "Keyboard target" }).FocusAsync();

            await target.Page.Keyboard.PressAsync("Escape");
            await target.Page.Keyboard.PressAsync("Enter");

            (await target.Page.EvaluateAsync<string[]>("() => window.calls.map(call => call.owner)")).Should().BeEmpty();
            (await target.Page.EvaluateAsync<bool[]>("() => window.keydowns")).Should().Equal(false, false);
        }
    }
}
