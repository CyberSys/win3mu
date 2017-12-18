﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sharp86;

namespace Win3muCore.Debugging
{
    public class DebuggerCommandExtensions
    {
        public DebuggerCommandExtensions(Machine machine)
        {
            _machine = machine;
        }

        Machine _machine;
                                          
        [DebuggerHelp("Dump call stack")]
        public void dump_call_stack(DebuggerCore debugger)
        {
            bool first = true;
            foreach (var e in _machine.StackWalker.WalkStack())
            {
                if (first)
                {
                    debugger.Write("→");
                    first = false;
                }
                else
                {
                    debugger.Write(" ");
                }
                debugger.WriteLine("  0x{0:X4}:{1:X4} [stack={2:X4}] {3}", e.csip.Hiword(), e.csip.Loword(), e.sp, e.name);
            }
        }

        [DebuggerHelp("Dump global heap")]
        public void dump_global_heap(DebuggerCore debugger)
        {
            foreach (var sel in _machine.GlobalHeap.AllSelectors)
            {
                debugger.WriteLine("0x{0:X4} - {1}", sel.selector, sel.name);
            }
        }

        [DebuggerHelp("Dump file handles")]
        public void dump_file_handles(DebuggerCore debugger)
        {
            foreach (var file in _machine.Dos.OpenFiles)
            {
                debugger.WriteLine("#{0} @{1} mode:{2} access:{3} share:{4} - {5} ({6})", 
                    file.handle, 
                    file.mode,
                    file.access,
                    file.share,
                    file.guestFilename, 
                    file.hostFilename);
            }
        }

        [DebuggerHelp("Calculate file location of memory address")]
        public void file_source(DebuggerCore debugger, FarPointer address)
        {
            // Get the selector
            var sel = _machine.GlobalHeap.GetSelector(address.Segment);
            if (sel == null || sel.allocation == null || sel.allocation.filename==null)
            {
                debugger.WriteLine("No associated source file");
                return;
            }

            uint offset = (uint)(((address.Segment >> 3) - sel.selectorIndex) << 16 | address.Offset);
            if (offset > sel.allocation.buffer.Length)
            {
                debugger.WriteLine("Address is past end of allocation");
                return;
            }

            debugger.WriteLine("VM address 0x{0:X4}:{1:X4} => {2}:{3:X8}", 
                address.Segment, address.Offset, 
                System.IO.Path.GetFileName(sel.allocation.filename), sel.allocation.fileoffset + offset);
        }

        [DebuggerHelp("Dump module list")]
        public void dump_modules(DebuggerCore debugger)
        {
            foreach (var m in _machine.ModuleManager.AllModules)
            {
                debugger.WriteLine("0x{0:X4} {1} {2}", m.hModule, m is Module32 ? "[32]" : "[16]", m.GetModuleName());
            }
        }

        [DebuggerHelp("Dump module info")]
        public void dump_module(DebuggerCore debugger, string modulename)
        {
            // Get the module
            var m = _machine.ModuleManager.GetModule(modulename);
            if (m==null)
            {
                debugger.WriteLine("Module '{0}' not found", modulename);
                return;
            }

            debugger.WriteLine("0x{0:X4} {1} {2}", m.hModule, m is Module32 ? "[32]" : "[16]", m.GetModuleName());

            // Dump related selectors
            debugger.WriteLine("\nSelectors:");
            var prefix = string.Format("Module '{0}'", modulename);
            foreach (var sel in _machine.GlobalHeap.AllSelectors.Where(x=>x.name.StartsWith(prefix, StringComparison.InvariantCultureIgnoreCase)))
            {
                debugger.WriteLine("0x{0:X4} - {1}", sel.selector, sel.name);
            }

            debugger.WriteLine("\nExports:");
            foreach (var ord in m.GetExports())
            {
                uint procAddress = m.GetProcAddress(ord);
                debugger.WriteLine("{0:X4} 0x{1:X4}:{2:X4} {3}", ord, procAddress.Hiword(), procAddress.Loword(), m.GetNameFromOrdinal(ord));
            }
        }

        [DebuggerHelp("Sets a break point that will trigger every time a wndproc is called")]
        public void bp_wndproc(DebuggerCore debugger)
        {
            debugger.AddBreakPoint(new WndProcBreakPoint());
        }
    }
}