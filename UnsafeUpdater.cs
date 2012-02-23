using System;
using Microsoft.SharePoint;

namespace Ironworks
{

    /// <summary>
    /// When paired with a <c>using</c> statement,
    /// allows for unsafe updates to happen to a <see cref="SPWeb"/>.
    /// Specifically, when the <c>UnsafeUpdater</c> is created,
    /// it takes note of whether unsafe updates were allowed
    /// or not, and then marks them as allowed.  Upon destruction,
    /// it will return the <see cref="SPWeb.AllowUnsafeUpdates"/>
    /// flag to its prior value.
    /// </summary>
    /// <example>
    /// <code>
    /// using (new UnsafeUpdater())
    /// {
    ///     // Do some unsafe updates
    /// }
    /// </code>
    /// </example>
    /// <remarks>
    /// Released into the public domain for any use as long as
    /// attribution is retained.  I'm not a lawyer, so don't sue me.
    /// Use my class if you want but don't be a jerk. :)
    /// 
    /// Copyright (C) 2010 Casey Liss
    /// cliss@ironworks.com
    /// </remarks>
    public sealed class UnsafeUpdater : IDisposable
    {

        #region Fields

        bool _updatesWereAllowed = false;
        SPWeb _web = null;

        #endregion Fields

        #region Public Methods

        /// <summary>
        /// Creates a new <c>UnsafeUpdater</c> and marks unsafe
        /// updates as enabled on the current web.
        /// </summary>
        public UnsafeUpdater()
            : this(SPContext.Current.Web)
        {
        }

        /// <summary>
        /// Creates a new <c>UnsafeUpdater</c> and marks unsafe
        /// updates as enabled on the provided web.
        /// </summary>
        /// <param name="web">Web to allow unsafe updates on.</param>
        public UnsafeUpdater(SPWeb web)
        {
            _web = web;
            _updatesWereAllowed = _web.AllowUnsafeUpdates;
            _web.AllowUnsafeUpdates = true;
        }

        /// <summary>
        /// Disposes of this object and returns the unsafe updates
        /// mode to the way it was prior to this object changing it.
        /// </summary>
        public void Dispose()
        {
            _web.AllowUnsafeUpdates = _updatesWereAllowed;
            GC.SuppressFinalize(this);
        }

        #endregion Public Methods

        #region Destructor

        /// <summary>
        /// Finalizes the object.  This should NOT be called.
        /// </summary>
        ~UnsafeUpdater()
        {
            throw new Exception("Did not Dispose() of " + this.GetType().Name + "!");
        }

        #endregion Destructor

    }
}
