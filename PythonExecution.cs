/////////////////////////////////////////////////////////////////////////////
// PythonExecution
// PythonExection<T>
//
// Classes to encapsulate and execute a Python script in .NET using IronPython
//
// This file is provided with no guarantee whatsoever.  In fact, it probably
// will break your code, and possibly accidentally hurt a bunny.  It's
// recommended for use as *inspiration* and nothing more.
//
// This file is, however, released under a use-it-for-whatever-even-
// commercial-stuff-but-if-stuff-breaks-it's-not-my-fault license.
//
// Use at your own risk.
/////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.IO;

using Microsoft.Scripting.Hosting;

using PY = IronPython.Hosting;

namespace IronPythonSandbox
{

    /// <summary>
    /// Encapsulates and executes a block of Python code
    /// </summary>
    public class PythonExecution
    {

        #region Fields

        /// <summary>Gets a flag indicating if the code has been executed or not</summary>
        public bool HasExecuted { get; protected set; }
        /// <summary>Gets the resultant exception, if any</summary>
        public Exception Exception { get; protected set; }
        /// <summary>Gets or sets the script to run</summary>
        public string Script { get; set; }
        /// <summary>Gets the contents of standard out</summary>
        public string StandardOut { get; protected set; }
        /// <summary>
        /// Gets or set a flag indicating if the parent assembly of 
        /// each of the <see cref="Parameters"/> should be loaded into
        /// the Python runtime.
        /// </summary>
        public bool LoadRequiredAssemblies { get; set; }
        /// <summary>
        /// Gets the result of the script
        /// </summary>
        virtual public object Result { get; protected set; }
        /// <summary>
        /// Gets or sets a dictionary of the parameters to pass.  For
        /// convenience, <see cref="AddParameter"/> is also provided.
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; }
        /// <summary>
        /// Gets or sets an <see cref="Action{T}"/> to execute on the 
        /// <see cref="ScriptSource"/> prior to execution.
        /// </summary>
        public Action<ScriptSource> PreExecutionSourceEdits { get; set; }
        /// <summary>
        /// Gets or sets an <see cref="Action{T}"/> to execute on the
        /// <see cref="ScriptScope"/> prior to execution.
        /// </summary>
        public Action<ScriptScope> PreExecutionScopeEdits { get; set; }

        #endregion Fields

        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="script">Script to execute</param>
        public PythonExecution(string script)
        {
            this.Script = script;
            this.Parameters = new Dictionary<string, object>();
        }

        /// <summary>
        /// Copy constructor.  Creates a runnable copy of the passed-in execution.
        /// </summary>
        /// <param name="source">Execution to copy</param>
        public PythonExecution(PythonExecution source) : this(source.Script)
        {
            this.LoadRequiredAssemblies = source.LoadRequiredAssemblies;
            this.Parameters = source.Parameters;
        }

        #endregion Constructors

        #region Public Methods

        /// <summary>
        /// Creates a runnable copy of this execution.
        /// </summary>
        /// <returns>Runnable copy of this execution</returns>
        public PythonExecution CreateRunnableCopy()
        {
            return new PythonExecution(this);
        }

        /// <summary>
        /// Adds a parameter to pass to the script
        /// </summary>
        /// <param name="name">Name of the parameter to be used in the script</param>
        /// <param name="value">Value of the parameter</param>
        public void AddParameter(string name, object value)
        {
            this.Parameters.Add(name, value);
        }

        /// <summary>
        /// Executes the script and stores the result in <see cref="Result"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the execution has already been run or if the script is not set.
        /// </exception>
        public void Execute()
        {
            if (this.HasExecuted)
            {
                throw new InvalidOperationException("Script has already been executed.");
            }
            else if (string.IsNullOrEmpty(this.Script))
            {
                throw new InvalidOperationException("Cannot execute script until script is specified.");
            }

            ScriptEngine engine = PY.Python.CreateEngine();

            // If the script refers to an assembly that isn't loaded, it will fail.
            // Thus, we can optionally load all the assemblies.
            if (this.LoadRequiredAssemblies)
            {
                foreach (object value in this.Parameters.Values)
                {
                    engine.Runtime.LoadAssembly(value.GetType().Assembly);
                }
            }

            ScriptScope scope = engine.CreateScope();
            var e = this.Parameters.GetEnumerator();
            while (e.MoveNext())
            {
                scope.SetVariable(e.Current.Key, e.Current.Value);
            }

            // This is necessary in nearly all cases, so just go ahead
            // and prepend it.
            ScriptSource source = engine.CreateScriptSourceFromString(
                "from System import * \n" + this.Script.Trim());

            using (MemoryStream ms = new MemoryStream())
            using (StringWriter sw = new StringWriter())
            {
                try
                {
                    engine.Runtime.IO.SetOutput(ms, sw);

                    // If we have any pre-execution steps, run them.
                    if (this.PreExecutionScopeEdits != null)
                    {
                        this.PreExecutionScopeEdits(scope);
                    }
                    if (this.PreExecutionSourceEdits != null)
                    {
                        this.PreExecutionSourceEdits(source);
                    }

                    DoExecution(source, scope);
                                        
                    this.StandardOut = sw.ToString();
                }
                catch (Exception ex)
                {
                    this.Exception = ex;
                }
                finally
                {
                    this.HasExecuted = true;
                }
            }
        }

        #endregion Public Methods

        #region Protected Methods

        /// <summary>
        /// Performs the actual execution.  Virtual so derived classes can
        /// override.
        /// </summary>
        /// <param name="source">Script source to execute</param>
        /// <param name="scope">Scope to execute within</param>
        virtual protected void DoExecution(ScriptSource source, ScriptScope scope)
        {
            this.Result = source.Execute(scope);
        }

        #endregion Protected Methods

    }

    /// <summary>
    /// Encapsulates and executes a block of Python code,
    /// with an expected return type.
    /// </summary>
    /// <typeparam name="T">Type of result to expect</typeparam>
    public class PythonExecution<T> : PythonExecution
    {

        #region Fields

        new public T Result { get; protected set; }

        #endregion Fields

        #region Constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="script">Script to run</param>
        public PythonExecution(string script)
            : base(script)
        {
        }

        /// <summary>
        /// Copy constructor.  Creates a runnable copy of the passed-in execution.
        /// </summary>
        /// <param name="source">Execution to copy.</param>
        public PythonExecution(PythonExecution<T> source)
            : base(source)
        {
        }

        #endregion Constructors

        #region Public and Protected Methods

        /// <summary>
        /// Creates a runnable copy of this execution.
        /// </summary>
        /// <returns>Runnable copy of this execution.</returns>
        public PythonExecution<T> CreateRunnableCopy()
        {
            return new PythonExecution<T>(this);
        }

        protected override void DoExecution(ScriptSource source, ScriptScope scope)
        {
            T result = source.Execute<T>(scope);
            object foo = source.Execute(scope);
            this.Result = source.Execute<T>(scope);
        }

        #endregion Public and Protected Methods

    }

}