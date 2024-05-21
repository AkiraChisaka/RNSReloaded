﻿using Reloaded.Hooks.Definitions;
using RNSReloaded.Interfaces.Structs;

namespace RNSReloaded;

// ReSharper disable InconsistentNaming
public unsafe class Hooks : IDisposable {
    public SLLVMVars* LLVMVars = null;

    private Utils utils;
    private WeakReference<IReloadedHooks> hooksRef;

    private delegate nint InitLLVMDelegate(SLLVMVars* vars);
    private IHook<InitLLVMDelegate>? initLLVMHook;

    private delegate void RunStartDelegate();
    private IHook<RunStartDelegate>? runStartHook;

    public event Action? OnRunStart;

    public Hooks(Utils utils, WeakReference<IReloadedHooks> hooksRef) {
        this.utils = utils;
        this.hooksRef = hooksRef;

        if (this.hooksRef.TryGetTarget(out var hooks)) {
            this.utils.Scan("E8 ?? ?? ?? ?? 48 8B 0D ?? ?? ?? ?? 8B 41 14", addr => {
                this.initLLVMHook = hooks.CreateHook<InitLLVMDelegate>(this.InitLLVMDetour, addr);
                this.initLLVMHook.Enable();
                this.initLLVMHook.Activate();
            });

            this.utils.Scan("48 83 EC 28 80 3D ?? ?? ?? ?? ?? 75 0C", addr => {
                this.runStartHook = hooks.CreateHook<RunStartDelegate>(this.RunStartDetour, addr);
                this.runStartHook.Enable();
                this.runStartHook.Activate();
            });
        }
    }

    public void Dispose() {
        this.initLLVMHook?.Disable();
    }

    private nint InitLLVMDetour(SLLVMVars* vars) {
        var orig = this.initLLVMHook!.OriginalFunction(vars);
        this.LLVMVars = vars;
        return orig;
    }

    private void RunStartDetour() {
        this.OnRunStart?.Invoke();
        this.runStartHook!.OriginalFunction();
    }
}
