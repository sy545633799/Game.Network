﻿using NLog;

namespace Server.Base.Log
{
	public class NLogAdapter: ALogDecorater
	{
		private readonly Logger logger = LogManager.GetLogger("Logger");

		public NLogAdapter(ALogDecorater decorater = null): base(decorater)
		{
		}

		public void Trace(string message)
		{
			this.logger.Trace(this.Decorate(message));
		}

		public void Warning(string message)
		{
			this.logger.Warn(this.Decorate(message));
		}

		public void Info(string message)
		{
			this.logger.Info(this.Decorate(message));
		}

		public void Debug(string message)
		{
			this.logger.Debug(this.Decorate(message));
		}

        public void Error(System.Exception exception)
        {
            logger.Error(exception);
        }


        public void Error(string message)
		{
			this.logger.Error(this.Decorate(message));
		}
	}
}