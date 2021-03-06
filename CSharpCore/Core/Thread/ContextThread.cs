﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace EngineNS.Thread
{
    public class ContextThread : CoreObjectBase
    {
        [ThreadStatic]
        public static ContextThread CurrentContext;
        public ContextThread()
        {
            Interval = 20;
        }
        List<object> mMonitorEnterObjects = new List<object>();
        public int MonitorEnter(object obj)
        {
            System.Threading.Monitor.Enter(obj);
            int index = mMonitorEnterObjects.Count;
            mMonitorEnterObjects.Add(obj);
            return index;
        }
        public bool MonitorExit(int index)
        {
            if (index<0 || index >= mMonitorEnterObjects.Count)
                return false;

            System.Threading.Monitor.Exit(mMonitorEnterObjects[index]);
            mMonitorEnterObjects[index] = null;
            return true;
        }
        public void ExitWhenFrameFinished()
        {
            for (int i = 0; i < mMonitorEnterObjects.Count; i++)
            {
                if(mMonitorEnterObjects[i]!=null)
                {
                    System.Threading.Monitor.Exit(mMonitorEnterObjects[i]);
                    Profiler.Log.WriteLine(Profiler.ELogTag.Warning, "Thread", $"Locker({mMonitorEnterObjects[i]}) is not released");
                }
            }
            mMonitorEnterObjects.Clear();
        }
        protected bool mIsRun = false;
        private bool mIsFinished = false;
        public int Interval
        {
            get;
            set;
        }
        protected System.Threading.Thread mThread;
        public string Name;
        public void FromCurrent(string name)
        {
            if (mThreadId == 0)
            {
                mThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
                if(CurrentContext==null)
                    CurrentContext = this;
                mIsRun = true;
                this.Name = name;
            }
            this.OnThreadStart();
        }
        public int TickFlag = 0;
        public delegate void FOnThreadTick(ContextThread ctx);
        public FOnThreadTick TickAction = null;
        public virtual bool StartThread(string name, FOnThreadTick action)
        {
            if (mIsRun)
                return false;
            Name = name;
            mIsRun = true;
            mIsFinished = false;
            mThread = new System.Threading.Thread(ThreadMain);
            mThread.Name = name;
            TickAction = action;
            mThread.Start();
            return true;
        }
        public virtual void StopThread(System.Action waitAction)
        {
            if (mIsRun == false)
                return;

            mIsRun = false;
            if (mThreadId == System.Threading.Thread.CurrentThread.ManagedThreadId)
            {
                System.Diagnostics.Debugger.Break();
                mThread = null;
                return;
            }
            while(mIsFinished==false)
            {
                if (waitAction != null)
                    waitAction();
                System.Threading.Thread.Sleep(5);
            }
            mThread = null;
        }
        protected int mThreadId = 0;
        public int ThreadId
        {
            get
            {
                return mThreadId;
            }
        }
        public bool IsFinished
        {
            get
            {
                return mIsFinished;
            }
        }
        private void ThreadMain()
        {
            CurrentContext = this;
            mThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
            OnThreadStart();
            while (mIsRun)
            {
                var time1 = Support.Time.GetTickCount();
                try
                {
                    Tick();
                }
                catch(Exception ex)
                {
                    Profiler.Log.WriteException(ex);
                }
                finally
                {
                    ContextThread.CurrentContext.ExitWhenFrameFinished();
                }
                var time2 = Support.Time.GetTickCount();
                if (time2 - time1 < Interval)
                    System.Threading.Thread.Sleep(Interval - (int)(time2 - time1));
            }
            mIsFinished = true;
            OnThreadExited();
            CurrentContext = null;
        }
        public virtual void Tick()
        {
            if(TickAction!=null)
            {
                TickAction(this);
            }
        }
        protected virtual void OnThreadStart()
        {

        }
        protected virtual void OnThreadExited()
        {

        }
        public int PriorityNum
        {
            get { return PriorityEvents.Count; }
        }
        public int AsyncNum
        {
            get { return AsyncEvents.Count; }
        }
        public int ContinueNum
        {
            get { return ContinueEvents.Count; }
        }
        [Browsable(false)]
        public Queue<Async.PostEvent> PriorityEvents
        {
            get;
        } = new Queue<Async.PostEvent>();
        [Browsable(false)]
        public Queue<Async.PostEvent> AsyncEvents
        {
            get;
        } = new Queue<Async.PostEvent>();
        [Browsable(false)]
        public Queue<Async.PostEvent> ContinueEvents
        {
            get;
        } = new Queue<Async.PostEvent>();
        public long LimitTime
        {
            get;
            set;
        } = 5 * 1000;
        private int LimitTimeScalar = 1;
        private bool TimeOut = false;
        public void TickAwaitEvent()
        {
            if(TimeOut)
            {
                LimitTimeScalar = 2 * LimitTimeScalar;
            }
            else
            {
                LimitTimeScalar = 1;
            }
            long limit = LimitTime * LimitTimeScalar;
            Async.PostEvent cur;
            var t1 = Support.Time.HighPrecision_GetTickCount();
            int count = 0;
            while(DoOnePriorityEvent(out cur))
            {
                count++;
                //var t2 = Support.Time.HighPrecision_GetTickCount();
                //if (t2 - t1 > limit)
                //{
                //    if (CEngine.Instance.EventPoster.IsThread(Async.EAsyncTarget.Logic))
                //    {
                //        Profiler.Log.WriteLine(Profiler.ELogTag.Warning, "Async", $"Logic Thread[Priority] is Blocked({t2 - t1}):{cur.Awaiter}");
                //    }
                //    TimeOut = true;
                //    CEngine.Instance.EventPoster.mRunOnPEAllocator.ReleaseObject(cur);
                //    return;
                //}
                //else
                {
                    CEngine.Instance.EventPoster.mRunOnPEAllocator.ReleaseObject(cur);
                }
            }
            //DoPriorityEvents();
            
            while (DoOneAsyncEvent(out cur))
            {
                var t2 = Support.Time.HighPrecision_GetTickCount();
                if(t2-t1> limit)
                {
                    if( CEngine.Instance.EventPoster.IsThread(Async.EAsyncTarget.Logic) )
                    {
                        Profiler.Log.WriteLine(Profiler.ELogTag.Warning, "Async", $"Logic Thread[Async] is Blocked({t2 - t1}):{cur.PostAction.ToString()}");

                        //if (cur.CallStackTrace != null)
                        //{
                        //    Profiler.Log.WriteLine(Profiler.ELogTag.Warning, "Async", $"StackInof=>{cur.CallStackTrace}");
                        //}
                    }
                    else if (CEngine.Instance.EventPoster.IsThread(Async.EAsyncTarget.Render))
                    {
                        Profiler.Log.WriteLine(Profiler.ELogTag.Warning, "Async", $"Render Thread[Async] is Blocked({t2 - t1}):{cur.PostAction.ToString()}");

                        //if (cur.CallStackTrace != null)
                        //{
                        //    Profiler.Log.WriteLine(Profiler.ELogTag.Warning, "Async", $"StackInof=>{cur.CallStackTrace}");
                        //}
                    }
                    TimeOut = true;
                    return;
                }
            }
            while(DoOneContnueEvent(out cur))
            {
                var t2 = Support.Time.HighPrecision_GetTickCount();
                if (t2 - t1 > limit)
                {
                    if (CEngine.Instance.EventPoster.IsThread(Async.EAsyncTarget.Logic))
                    {
                        if(t2 - t1>200000)
                        {
                            Profiler.Log.WriteLine(Profiler.ELogTag.Warning, "Async", $"Logic Thread[Continue] is Blocked({t2 - t1}):{cur.Awaiter}");
                            //if(cur.CallStackTrace!=null)
                            //{
                            //    Profiler.Log.WriteLine(Profiler.ELogTag.Warning, "Async", $"StackInof=>{cur.CallStackTrace}");
                            //}
                        }
                    }
                    TimeOut = true;
                    return;
                }
            }
            TimeOut = false;
        }
        public bool DoOnePriorityEvent(out Async.PostEvent oe)
        {
            oe = null;
            Async.PostEvent e;
            lock (PriorityEvents)
            {
                if (PriorityEvents.Count == 0)
                    return false;
                e = PriorityEvents.Dequeue();
            }

            try
            {
                if (e.PostAction != null)
                {
                    var ret = e.PostAction();
                    var caw = e.Awaiter as Async.TaskAwaiter;
                    if (caw != null)
                        caw.SetResult(ret);
                }
                ExecuteContinue(e.Awaiter);
            }
            catch (Exception ex)
            {
                Profiler.Log.WriteException(ex);
                e.ExceptionInfo = ex;
            }
            oe = e;
            return true;
        }
        //protected void DoPriorityEvents()
        //{
        //    while(PriorityEvents.Count>0)
        //    {
        //        Async.PostEvent e;
        //        lock (PriorityEvents)
        //        {
        //            e = PriorityEvents.Dequeue();
        //        }
        //        try
        //        {
        //            if (e.PostAction != null)
        //            {
        //                var ret = e.PostAction();
        //                var caw = e.Awaiter as Async.TaskAwaiter;
        //                if(caw!=null)
        //                    caw.SetResult(ret);
        //            }
        //            ExecuteContinue(e.Awaiter);
        //        }
        //        catch (Exception ex)
        //        {
        //            Profiler.Log.WriteException(ex);
        //            e.ExceptionInfo = ex;
        //        }
        //    }
        //}
        protected bool DoOneAsyncEvent(out Async.PostEvent oe)
        {
            oe = null;
            Async.PostEvent e;
            lock (AsyncEvents)
            {
                if (AsyncEvents.Count == 0)
                    return false;
                e = AsyncEvents.Dequeue();
            }
            try
            {
                var ret = e.PostAction();
                var caw = e.Awaiter as Async.TaskAwaiter;
                caw.SetResult(ret);
            }
            catch (Exception ex)
            {
                Profiler.Log.WriteException(ex);
                e.ExceptionInfo = ex;
                var caw = e.Awaiter as Async.TaskAwaiter;
                caw.SetResult(null);
            }
            if(e.Tag==null)
            {
                if(e.ContinueThread != null)
                {
                    lock (e.ContinueThread.ContinueEvents)
                    {
                        e.ContinueThread.ContinueEvents.Enqueue(e);
                    }
                }
            }

            oe = e;
            return true;
        }
        protected bool DoOneContnueEvent(out Async.PostEvent oe)
        {
            oe = null;
            Async.PostEvent e;
            lock (ContinueEvents)
            {
                if (ContinueEvents.Count == 0)
                    return false;
                e = ContinueEvents.Dequeue();
            }
            try
            {
                ExecuteContinue(e.Awaiter);
            }
            catch (Exception ex)
            {
                Profiler.Log.WriteException(ex);
                e.ExceptionInfo = ex;
            }
            oe = e;
            return true;
        }
        private void ExecuteContinue(Async.TaskAwaiter awaiter)
        {
            if (awaiter!=null && awaiter.IsContinueAction())
            {
#if PWindow
                var saved = System.Threading.SynchronizationContext.Current;
                System.Threading.SynchronizationContext.SetSynchronizationContext(null);
                awaiter.DoContinueAction();
                System.Threading.SynchronizationContext.SetSynchronizationContext(saved);
#else
                awaiter.DoContinueAction();
#endif
            }
        }

        public bool IsThisThread()
        {
            return (this.ThreadId == System.Threading.Thread.CurrentThread.ManagedThreadId);
        }

        #region SDK
        [System.Runtime.InteropServices.DllImport(CoreObjectBase.ModuleNC, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public extern static void Thread_StartLogicThread();
        [System.Runtime.InteropServices.DllImport(CoreObjectBase.ModuleNC, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public extern static void Thread_StartRHIThread();
        [System.Runtime.InteropServices.DllImport(CoreObjectBase.ModuleNC, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public extern static void Thread_StartIOThread();
        #endregion
    }
}
