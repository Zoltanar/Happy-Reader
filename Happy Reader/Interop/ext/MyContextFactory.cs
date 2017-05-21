using System;
using System.Collections.Generic;
using System.Linq;
using static Happy_Reader.StaticMethods;
// ReSharper disable All
#pragma warning disable IDE1006 // Naming Styles

namespace Happy_Reader.Interop
{
    class MyContextFactory : ContextFactory
    {

        private readonly TextHook textHook;

        public MyContextFactory(TextHook textHook)
        {
            this.textHook = textHook;
        }

        public enum NewContextsBehavior
        {
            ALLOW,
            IGNORE,
            SWITCH_TO_NEW,
            SMART
        }

        private volatile NewContextsBehavior _newContextsBehavior = NewContextsBehavior.SMART;
        public NewContextsBehavior newContextsBehavior
        {
            get
            {
                return _newContextsBehavior;
            }
            set
            {
                _newContextsBehavior = value;
                Session.newContextsBehavior = value;
            }
        }

        private bool hasSpecialContexts()
        {
            return textHook.getContexts().Any((c) => isContextSpecial(c.name));
        }

        public void onConnected()
        {
            _newContextsBehavior = Session.newContextsBehavior;
        }

        public TextHookContext create(int id, string name, int hook, int context, int subcontext, int status)
        {
            /*bool isEnabled;
            if (!Session.TryGetContextEnabled(context, subcontext, out isEnabled))
            {
                if (newContextsBehavior == NewContextsBehavior.SMART)
                {
                    isEnabled = getSmartEnabled(name);
                }
                else
                {
                    isEnabled = newContextsBehavior != NewContextsBehavior.IGNORE;
                }
            }*/
            return new MyContext(id, name, hook, context, subcontext, status, true);
        }

        private static readonly HashSet<string> genericContexts = new HashSet<string> {
            "GetTextExtentPoint32A",
            "GetGlyphOutlineA",
            "ExtTextOutA",
            "GetCharABCWidthsA",
            "DrawTextA",
            "DrawTextExA",
            "GetTextExtentPoint32W",
            "GetGlyphOutlineW",
            "ExtTextOutW",
            "TextOutW",
            "GetCharABCWidthsW",
            "DrawTextW",
            "DrawTextExW"
        };

        public static bool isContextSpecial(string contextName)
        {
            return !(genericContexts.Contains(contextName) || contextName.Contains("strlen"));
        }

        private bool getSmartEnabled(string name)
        {
            bool isSpecial = isContextSpecial(name);
            return isSpecial || !hasSpecialContexts();
        }

        public void setNewContextsBehavior(string behavior)
        {
            switch (behavior)
            {
                case "ignore":
                    newContextsBehavior = NewContextsBehavior.IGNORE;
                    break;
                case "switchto":
                    newContextsBehavior = NewContextsBehavior.SWITCH_TO_NEW;
                    break;
                case "smart":
                    newContextsBehavior = NewContextsBehavior.SMART;
                    break;
                default:
                    newContextsBehavior = NewContextsBehavior.ALLOW;
                    break;
            }
        }

        internal string getNewContextsBehaviorAsString()
        {
            switch (newContextsBehavior)
            {
                case NewContextsBehavior.ALLOW:
                    return "allow";
                case NewContextsBehavior.IGNORE:
                    return "ignore";
                case NewContextsBehavior.SWITCH_TO_NEW:
                    return "switchto";
                case NewContextsBehavior.SMART:
                    return "smart";
                default:
                    throw new Exception("unknown value");
            }
        }

        internal List<int> disableContextsIfNeeded(TextHookContext ctx)
        {
            List<int> disabledContexts = null;
            switch (newContextsBehavior)
            {
                case NewContextsBehavior.SWITCH_TO_NEW:
                    disabledContexts = new List<int>();
                    foreach (var ctx2 in textHook.getContexts())
                    {
                        if (ctx2.internalId < ctx.internalId)
                        {
                            if ((ctx2 as MyContext).enabled)
                            {
                                (ctx2 as MyContext).enabled = false;
                                disabledContexts.Add(ctx2.id);
                            }
                        }
                    }
                    break;
                case NewContextsBehavior.SMART:
                    if (isContextSpecial(ctx.name))
                    {
                        disabledContexts = new List<int>();
                        foreach (var ctx2 in textHook.getContexts())
                        {
                            if (ctx2.internalId < ctx.internalId && !isContextSpecial(ctx2.name))
                            {
                                if ((ctx2 as MyContext).enabled)
                                {
                                    (ctx2 as MyContext).enabled = false;
                                    disabledContexts.Add(ctx2.id);
                                }
                            }
                        }
                    }
                    break;
            }
            return disabledContexts;
        }
    }
#pragma warning restore IDE1006 // Naming Styles
}
