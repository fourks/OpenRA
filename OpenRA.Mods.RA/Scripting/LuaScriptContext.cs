﻿#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LuaInterface;

namespace OpenRA.Mods.RA.Scripting
{
	public class LuaScriptContext : IDisposable
	{
		public Lua Lua { get; private set; }

		public LuaScriptContext()
		{
			Log.Write("debug", "Creating Lua script context");
			Lua = new Lua();
		}

		public void RegisterObject(object target, string tableName, bool exposeAllMethods)
		{
			Log.Write("debug", "Registering object {0}", target);

			if (tableName != null && Lua.GetTable(tableName) == null)
				Lua.NewTable(tableName);

			var type = target.GetType();

			var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);
			RegisterMethods(tableName, target, methods, exposeAllMethods);
		}

		public void RegisterType(Type type, string tableName, bool exposeAllMethods)
		{
			Log.Write("debug", "Registering type {0}", type);

			if (tableName != null && Lua.GetTable(tableName) == null)
				Lua.NewTable(tableName);

			var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static);
			RegisterMethods(tableName, null, methods, exposeAllMethods);
		}

		void RegisterMethods(string tableName, object target, IEnumerable<MethodInfo> methods, bool allMethods)
		{
			foreach (var method in methods)
			{
				string methodName;

				var attr = method.GetCustomAttributes<LuaGlobalAttribute>(true).FirstOrDefault();
				if (attr == null)
				{
					if (allMethods)
						methodName = method.Name;
					else
						continue;
				}
				else
					methodName = attr.Name ?? method.Name;

				var methodTarget = method.IsStatic ? null : target;

				if (tableName != null)
					Lua.RegisterFunction(tableName + "." + methodName, methodTarget, method);
				else
					Lua.RegisterFunction(methodName, methodTarget, method);
			}
		}

		void LogException(Exception e)
		{
			Game.Debug("{0}", e.Message);
			Game.Debug("See debug.log for details");
			Log.Write("debug", "{0}", e);
		}

		public void LoadLuaScripts(Func<string, string> getFileContents, params string[] files)
		{
			foreach (var file in files)
			{
				try
				{
					Log.Write("debug", "Loading Lua script {0}", file);
					var content = getFileContents(file);
					Lua.DoString(content, file);
				}
				catch (Exception e)
				{
					LogException(e);
				}
			}
		}

		public object[] InvokeLuaFunction(string name, params object[] args)
		{
			try
			{
				var function = Lua.GetFunction(name);
				if (function == null)
					return null;
				return function.Call(args);
			}
			catch (Exception e)
			{
				LogException(e);
				return null;
			}
		}

		public void Dispose()
		{
			if (Lua == null)
				return;

			GC.SuppressFinalize(this);
			Lua.Dispose();
			Lua = null;
		}

		~LuaScriptContext()
		{
			Dispose();
		}
	}
}
