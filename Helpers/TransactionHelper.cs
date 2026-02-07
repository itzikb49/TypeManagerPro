// TransactionHelper.cs
using System;
using Autodesk.Revit.DB;

namespace TypeManagerPro.Helpers
{
    /// <summary>
    /// Helper for managing Revit transactions with automatic logging
    /// </summary>
    public class TransactionHelper : IDisposable
    {
        #region Private Fields

        private readonly Transaction _transaction;
        private readonly string _transactionName;
        private readonly Logger.LogCategory _logCategory;
        private bool _committed = false;
        private bool _disposed = false;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new transaction with automatic logging
        /// </summary>
        /// <param name="document">Revit document</param>
        /// <param name="transactionName">Transaction name</param>
        /// <param name="logCategory">Log category for this transaction</param>
        public TransactionHelper(Document document, string transactionName,
            Logger.LogCategory logCategory = Logger.LogCategory.Main)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            if (string.IsNullOrEmpty(transactionName))
                throw new ArgumentNullException(nameof(transactionName));

            _transactionName = transactionName;
            _logCategory = logCategory;

            _transaction = new Transaction(document, transactionName);

            Logger.Info(_logCategory, $"Transaction created: '{_transactionName}'");
        }

        #endregion

        #region Transaction Methods

        /// <summary>
        /// Starts the transaction
        /// </summary>
        public TransactionStatus Start()
        {
            try
            {
                Logger.Info(_logCategory, $"Transaction starting: '{_transactionName}'");

                TransactionStatus status = _transaction.Start();

                Logger.Info(_logCategory,
                    $"Transaction started: '{_transactionName}' (Status: {status})");

                return status;
            }
            catch (Exception ex)
            {
                Logger.Error(_logCategory,
                    $"Failed to start transaction: '{_transactionName}'", ex);
                throw;
            }
        }

        /// <summary>
        /// Commits the transaction
        /// </summary>
        public TransactionStatus Commit()
        {
            try
            {
                Logger.Info(_logCategory, $"Transaction committing: '{_transactionName}'");

                TransactionStatus status = _transaction.Commit();
                _committed = true;

                Logger.Info(_logCategory,
                    $"Transaction committed: '{_transactionName}' (Status: {status})");

                return status;
            }
            catch (Exception ex)
            {
                Logger.Error(_logCategory,
                    $"Failed to commit transaction: '{_transactionName}'", ex);
                throw;
            }
        }

        /// <summary>
        /// Rolls back the transaction
        /// </summary>
        public TransactionStatus RollBack()
        {
            try
            {
                Logger.Warning(_logCategory,
                    $"Transaction rolling back: '{_transactionName}'");

                TransactionStatus status = _transaction.RollBack();

                Logger.Warning(_logCategory,
                    $"Transaction rolled back: '{_transactionName}' (Status: {status})");

                return status;
            }
            catch (Exception ex)
            {
                Logger.Error(_logCategory,
                    $"Failed to rollback transaction: '{_transactionName}'", ex);
                throw;
            }
        }

        /// <summary>
        /// Gets the transaction name
        /// </summary>
        public string GetName()
        {
            return _transaction.GetName();
        }

        /// <summary>
        /// Gets the transaction status
        /// </summary>
        public TransactionStatus GetStatus()
        {
            return _transaction.GetStatus();
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Disposes the transaction - automatically rolls back if not committed
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            try
            {
                // If transaction was started but not committed, roll back
                if (_transaction.GetStatus() == TransactionStatus.Started && !_committed)
                {
                    Logger.Warning(_logCategory,
                        $"Transaction not committed - auto-rollback: '{_transactionName}'");

                    _transaction.RollBack();
                }

                _transaction?.Dispose();

                Logger.Info(_logCategory, $"Transaction disposed: '{_transactionName}'");
            }
            catch (Exception ex)
            {
                Logger.Error(_logCategory,
                    $"Error disposing transaction: '{_transactionName}'", ex);
            }
            finally
            {
                _disposed = true;
            }
        }

        #endregion

        #region Static Helper Methods

        /// <summary>
        /// Executes an action within a transaction with automatic error handling
        /// </summary>
        public static bool ExecuteInTransaction(Document document, string transactionName,
            Action action, Logger.LogCategory logCategory = Logger.LogCategory.Main)
        {
            if (document == null || action == null)
                return false;

            using (var trans = new TransactionHelper(document, transactionName, logCategory))
            {
                try
                {
                    trans.Start();

                    action();

                    trans.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    Logger.Error(logCategory,
                        $"Transaction failed: '{transactionName}'", ex);

                    try
                    {
                        trans.RollBack();
                    }
                    catch (Exception rollbackEx)
                    {
                        Logger.Error(logCategory,
                            $"Rollback failed for transaction: '{transactionName}'", rollbackEx);
                    }

                    return false;
                }
            }
        }

        /// <summary>
        /// Executes a function within a transaction with automatic error handling
        /// Returns the result or default value on failure
        /// </summary>
        public static T ExecuteInTransaction<T>(Document document, string transactionName,
            Func<T> function, T defaultValue = default(T),
            Logger.LogCategory logCategory = Logger.LogCategory.Main)
        {
            if (document == null || function == null)
                return defaultValue;

            using (var trans = new TransactionHelper(document, transactionName, logCategory))
            {
                try
                {
                    trans.Start();

                    T result = function();

                    trans.Commit();
                    return result;
                }
                catch (Exception ex)
                {
                    Logger.Error(logCategory,
                        $"Transaction failed: '{transactionName}'", ex);

                    try
                    {
                        trans.RollBack();
                    }
                    catch (Exception rollbackEx)
                    {
                        Logger.Error(logCategory,
                            $"Rollback failed for transaction: '{transactionName}'", rollbackEx);
                    }

                    return defaultValue;
                }
            }
        }

        #endregion
    }
}