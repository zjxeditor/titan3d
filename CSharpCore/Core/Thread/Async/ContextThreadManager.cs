﻿#define HAS_DebugInfo
using System;
using System.Collections.Generic;
using System.Text;

namespace EngineNS.Thread.Async
{
    public enum EAsyncTarget
    {
        AsyncIO,
        Physics,
        Logic,
        Render,
        Main,
        Editor,
        AsyncEditor,
        AsyncEditorSlow,
        WaitCreated,
        TPools,//塞在这个队列的异步处理，必须相互之间没有依赖，可以并行，因为线程池会有多条线程去取出来执行
    }
    public delegate object FPostEvent();
    public delegate System.Threading.Tasks.Task<T> FAsyncPostEvent<T>();
    public delegate T FPostEventReturn<T>();
    internal enum EAsyncType
    {
        Normal,
        Semaphore,
        AsyncIOEmpty,
        ParallelTasks,
        Delay,
    }
    public class PostEvent
    {
        internal EAsyncType AsyncType = EAsyncType.Normal;
        public FPostEvent PostAction;
        public TaskAwaiter Awaiter;
        public ContextThread ContinueThread;
        public ContextThread AsyncTarget;
        
        public ASyncSemaphore Tag = null;
        public System.Exception ExceptionInfo = null;
        public System.Diagnostics.StackTrace CallStackTrace;
        public void Reset()
        {
            AsyncType = EAsyncType.Normal;
            PostAction = null;
            Awaiter = null;
            ContinueThread = null;
            AsyncTarget = null;
            Tag = null;
            ExceptionInfo = null;
            CallStackTrace = null;
        }
    }
    public class ContextThreadManager
    {
        public static bool ImmidiateMode = false;

        public bool IsThread(EAsyncTarget target)
        {
            var ctx = ContextThread.CurrentContext;
            switch (target)
            {
                case EAsyncTarget.AsyncIO:
                    return ctx == ContextAsyncIO;
                case EAsyncTarget.Physics:
                    return ctx == ContextPhysics;
                case EAsyncTarget.Logic:
                    return ctx == ContextLogic;
                case EAsyncTarget.Render:
                    return ctx == ContextRHI;
                case EAsyncTarget.Main:
                    return ctx == ContextMain;
                case EAsyncTarget.Editor:
                    return ctx == ContextEditor;
                case EAsyncTarget.AsyncEditor:
                    return ctx == ContextAsyncEditor;
                case EAsyncTarget.AsyncEditorSlow:
                    return ctx == ContextAsyncEditorSlow;
                case EAsyncTarget.WaitCreated:
                    return ctx == ContextWaitCreated;
                case EAsyncTarget.TPools:
                    break;
            }
            return false;
        }

        public class PostEventAllocator : PooledObject<PostEvent>
        {
            protected override bool OnObjectRelease(PostEvent obj)
            {
                obj.Reset();
                return true;
            }
        }
        public PostEventAllocator mRunOnPEAllocator = new PostEventAllocator();

        internal ContextThread ContextAsyncIO = null;
        internal ContextThread ContextLogic = null;
        internal ContextThread ContextRender = null;
        internal ContextThread ContextRHI = null;
        internal ContextThread ContextMain = null;
        internal ContextThread ContextEditor = null;
        internal ContextThread ContextAsyncEditor = null;
        internal ContextThread ContextAsyncEditorSlow = null;
        internal ContextThread ContextPhysics = null;
        internal ContextThread ContextWaitCreated = null;
        internal Queue<PostEvent> TPoolEvents = new Queue<PostEvent>();
        internal System.Threading.AutoResetEvent mTPoolTrigger = new System.Threading.AutoResetEvent(false);
        internal ThreadPool[] ContextPools;
        internal List<PostEvent> AsyncIOEmptys = new List<PostEvent>();
        internal static ContextThreadManager Instance
        {
            get;
        } = new ContextThreadManager();

        public int PooledThreadNum
        {
            get { return ContextPools.Length; }
        }

        public struct MTPSegment
        {
            public int Start;
            public int End;
        }
        private MTPSegment[] GetMultThreadSegement(int num)
        {
            int stride = num / ContextPools.Length;
            int remain = num % ContextPools.Length;
            var result = new MTPSegment[ContextPools.Length];
            int startIndex = 0;
            for (int i = 0; i < ContextPools.Length; i++)
            {
                result[i].Start = startIndex;
                startIndex += stride;
                result[i].End = startIndex;
            }
            result[ContextPools.Length - 1].End += remain;
            return result;
        }
        public delegate void FMTS_DoForeach(int index);
        public delegate void FMTS_DoForeach2(int index, Thread.ASyncSemaphore smp);
        public bool EnableMTForeach = true;
        public void MTS_Foreach(int num, FMTS_DoForeach action)
        {
            if(EnableMTForeach==false)
            {
                for(int i=0;i<num;i++)
                {
                    action(i);
                }
                return;
            }
            int stride = num / ContextPools.Length;
            int remain = num % ContextPools.Length;
            unsafe
            {
                MTPSegment* segs = stackalloc MTPSegment[ContextPools.Length];

                int startIndex = 0;
                for (int i = 0; i < ContextPools.Length; i++)
                {
                    segs[i].Start = startIndex;
                    startIndex += stride;
                    segs[i].End = startIndex;
                }
                segs[ContextPools.Length - 1].End += remain;

                var smp = EngineNS.Thread.ASyncSemaphore.CreateSemaphore(ContextPools.Length, new System.Threading.AutoResetEvent(false));
                for (int i = 0; i < ContextPools.Length; i++)
                {
                    MTPSegment curSeg = segs[i];
                    CEngine.Instance.EventPoster.RunOn(() =>
                    {
                        for (int j = curSeg.Start; j < curSeg.End; j++)
                        {
                            try
                            {
                                action(j);
                            }
                            catch(Exception ex)
                            {
                                Profiler.Log.WriteException(ex);
                            }
                        }
                        smp.Release();
                        return null;
                    }, Thread.Async.EAsyncTarget.TPools);
                }
                smp.Wait(int.MaxValue);
            }            
        }
        public async System.Threading.Tasks.Task AwaitMTS_Foreach(int num, FMTS_DoForeach2 action)
        {
            int stride = num / ContextPools.Length;
            int remain = num % ContextPools.Length;
            var smp = EngineNS.Thread.ASyncSemaphore.CreateSemaphore(ContextPools.Length, new System.Threading.AutoResetEvent(false));
            unsafe
            {
                MTPSegment* segs = stackalloc MTPSegment[ContextPools.Length];

                int startIndex = 0;
                for (int i = 0; i < ContextPools.Length; i++)
                {
                    segs[i].Start = startIndex;
                    startIndex += stride;
                    segs[i].End = startIndex;
                }
                segs[ContextPools.Length - 1].End += remain;

                for (int i = 0; i < ContextPools.Length; i++)
                {
                    MTPSegment curSeg = segs[i];
                    CEngine.Instance.EventPoster.RunOn(() =>
                    {
                        for (int j = curSeg.Start; j < curSeg.End; j++)
                        {
                            try
                            {
                                action(j, smp);
                            }
                            catch (Exception ex)
                            {
                                Profiler.Log.WriteException(ex);
                            }
                        }
                        smp.Release();
                        return null;
                    }, Thread.Async.EAsyncTarget.TPools);
                }
            }
            await smp.Await();
        }
        public void StartPools(int count)
        {
            ContextPools = new ThreadPool[count];
            for(int i=0;i<count;i++)
            {
                ContextPools[i] = new ThreadPool();
                ContextPools[i].StartThread($"TPool{i}", null);
            }
        }
        public void StopPools()
        {
            foreach(var i in ContextPools)
            {
                i.StopThread(null);
            }
        }
        public ContextThread GetContext(EAsyncTarget target)
        {
            ContextThread ctx = null;
            switch (target)
            {
                case EAsyncTarget.AsyncIO:
                    ctx = ContextAsyncIO;
                    break;
                case EAsyncTarget.Physics:
                    ctx = ContextPhysics;
                    break;
                case EAsyncTarget.Logic:
                    ctx = ContextLogic;
                    break;
                case EAsyncTarget.Render:
                    ctx = ContextRHI;
                    break;
                case EAsyncTarget.Main:
                    ctx = ContextMain;
                    break;
                case EAsyncTarget.Editor:
                    ctx = ContextEditor;
                    break;
                case EAsyncTarget.AsyncEditor:
                    ctx = ContextAsyncEditor;
                    break;
                case EAsyncTarget.AsyncEditorSlow:
                    ctx = ContextAsyncEditorSlow;
                    break;
                case EAsyncTarget.WaitCreated:
                    ctx = ContextWaitCreated;
                    break;
                case EAsyncTarget.TPools:
                    break;
            }
            return ctx;
        }
        public void RunOn<T>(FAsyncPostEvent<T> evt, EAsyncTarget target = EAsyncTarget.AsyncIO)
        {
            if (ContextThreadManager.ImmidiateMode)
            {
                evt();
                return;
            }

            ContextThread ctx = GetContext(target);
            FPostEvent ue = () =>
            {
                return evt();
            };
            var eh = mRunOnPEAllocator.QueryObjectSync();
            eh.PostAction = ue;
            eh.ContinueThread = null;
            eh.AsyncType = EAsyncType.ParallelTasks;
            if (ctx != null)
            {
                lock (ctx.PriorityEvents)
                {
                    ctx.PriorityEvents.Enqueue(eh);
                }
            }
            else
            {
                lock (TPoolEvents)
                {
                    TPoolEvents.Enqueue(eh);
                    mTPoolTrigger.Set();
                }
            }
        }
        public void RunOn(FPostEvent evt, EAsyncTarget target = EAsyncTarget.AsyncIO)
        {
            ContextThread ctx = GetContext(target);

            var eh = mRunOnPEAllocator.QueryObjectSync();
            
            eh.PostAction = evt;
            eh.ContinueThread = null;
            eh.AsyncType = EAsyncType.ParallelTasks;
            if (ctx != null)
            {
                lock (ctx.PriorityEvents)
                {
                    ctx.PriorityEvents.Enqueue(eh);
                }
            }
            else
            {
                lock (TPoolEvents)
                {
                    TPoolEvents.Enqueue(eh);
                    mTPoolTrigger.Set();
                }
            }
        }
        public async System.Threading.Tasks.Task DelayTime(int time)
        {
            var eh = new PostEvent();
            eh.PostAction = null;
            eh.ContinueThread = ContextThread.CurrentContext;
            eh.AsyncType = EAsyncType.Delay;
            eh.PostAction = () =>
            {
                System.Threading.Thread.Sleep(time);
                return null;
            };
            var source = new System.Threading.Tasks.TaskCompletionSource<object>();
            await source.Task.AwaitPost(eh);
        }
        public async System.Threading.Tasks.Task<T> Post<T>(FPostEventReturn<T> evt, EAsyncTarget target = EAsyncTarget.AsyncIO)
        {
            var eh = new PostEvent();
            ContextThread ctx = GetContext(target);
            eh.AsyncTarget = ctx;
            eh.ContinueThread = ContextThread.CurrentContext;
            if(eh.ContinueThread == this.ContextRHI)
            {
                switch(eh.ContinueThread.TickFlag)
                {
                    case 1:
                        eh.ContinueThread = this.ContextMain;
                        break;
                    case 2:
                        eh.ContinueThread = this.ContextRHI;
                        break;
                    case 3:
                        eh.ContinueThread = this.ContextEditor;
                        break;
                }
            }
            FPostEvent ue = () =>
            {
                try
                {
                    var result = evt();
                    return result;
                }
                catch(Exception ex)
                {
                    Profiler.Log.WriteException(ex);
                    return default(T);
                }
            };
            eh.PostAction = ue;
            var source = new System.Threading.Tasks.TaskCompletionSource<object>();
            object ret = await source.Task.AwaitPost(eh);
            try
            {
                return (T)ret;
            }
            catch
            {
                return default(T);
            }
        }
        public async System.Threading.Tasks.Task AwaitSemaphore(ASyncSemaphore smp)
        {
            if(smp==null)
            {
                Profiler.Log.WriteLine(Profiler.ELogTag.Error, "", $"AwaitSemaphore is null");
                return;
            }
            var eh = new PostEvent();
            eh.PostAction = null;
            eh.AsyncTarget = null;
            eh.ContinueThread = ContextThread.CurrentContext;
            eh.AsyncType = EAsyncType.Semaphore;
            eh.Tag = smp;
            smp.PostEvent = eh;

            var source = new System.Threading.Tasks.TaskCompletionSource<object>();
            await source.Task.AwaitPost(eh);
            smp = null;
        }
        public async System.Threading.Tasks.Task AwaitAsyncIOEmpty()
        {
            var eh = new PostEvent();
            eh.PostAction = null;
            eh.AsyncTarget = null;
            eh.ContinueThread = ContextThread.CurrentContext;
            eh.AsyncType = EAsyncType.AsyncIOEmpty;
            eh.Tag = null;

            var source = new System.Threading.Tasks.TaskCompletionSource<object>();
            await source.Task.AwaitPost(eh);
        }
    }

    public class TaskAwaiter : System.Runtime.CompilerServices.INotifyCompletion
    {
        public System.Threading.Tasks.Task task;
        PostEvent PEvent;
        private Action ContinueAction;
        private object Result;
        private bool FinishedContinue = false;
        public override string ToString()
        {
            return ContinueAction.ToString();
        }
        public void SetResult(object r)
        {
            Result = r;
            //FinishedContinue = true;
        }
        public bool IsContinueAction()
        {
            return ContinueAction != null;
        }
        public void DoContinueAction()
        {
            //var t1 = Support.Time.HighPrecision_GetTickCount();
            //ContinueAction();
            //var t2 = Support.Time.HighPrecision_GetTickCount();
            //if(CEngine.Instance.EventPoster.IsThread(EAsyncTarget.Logic) && t2-t1>10000)
            //{
            //    int xxx = 0;
            //}
            ContinueAction();
            FinishedContinue = true;
        }
        public TaskAwaiter(System.Threading.Tasks.Task task, PostEvent pe)
        {
            PEvent = pe;
            pe.Awaiter = this;
            this.task = task;
        }
        public TaskAwaiter GetAwaiter()
        {
            return this;
        }
        public void OnCompleted(Action continuation)
        {
            bool isTargetIsCurrent = false;
            if (PEvent.AsyncType == EAsyncType.Normal)
            {
                if (ContextThread.CurrentContext == null)
                    isTargetIsCurrent = false;
                else
                    isTargetIsCurrent = PEvent.AsyncTarget == ContextThread.CurrentContext;
            }
            if (ContextThreadManager.ImmidiateMode || isTargetIsCurrent)
            {
                if (PEvent != null && PEvent.PostAction != null)
                {
                    var ret = PEvent.PostAction();
                    Result = ret;
                }
                ContinueAction = continuation;
                DoContinueAction();
                return;
            }
#if HAS_DebugInfo
            PEvent.CallStackTrace = new System.Diagnostics.StackTrace(2);
#endif
            PEvent.Awaiter.ContinueAction = continuation;
            switch (PEvent.AsyncType)
            {
                case EAsyncType.Normal:
                    {
                        lock (PEvent.AsyncTarget.AsyncEvents)
                        {
                            PEvent.AsyncTarget.AsyncEvents.Enqueue(PEvent);
                        }
                    }
                    break;
                case EAsyncType.AsyncIOEmpty:
                    {
                        System.Diagnostics.Debug.Assert(PEvent.AsyncTarget == null);
                        System.Diagnostics.Debug.Assert(PEvent.PostAction == null);
                        lock (ContextThreadManager.Instance.AsyncIOEmptys)
                        {
                            ContextThreadManager.Instance.AsyncIOEmptys.Add(PEvent);
                        }
                    }
                    break;
                case EAsyncType.Semaphore:
                    {
                        if (PEvent.Tag.GetCount() == 0)
                        {
                            //EqueueContinue做了防止重复Enqueue的处理
                            //如果Release导致提前Enqueue了，这里就不会真的在入队列一次
                            //否则会出现已经完成的任务再转换的异常
                            //为什么这里还要入队一次，因为有低概率在Release的时候，PostEvent等待任务
                            //依然没有赋值好，这里做一次擦屁股的处理
                            PEvent.Tag.EqueueContinue();
                        }
                    }
                    break;
                case EAsyncType.ParallelTasks:
                    {
                        lock (ContextThreadManager.Instance.TPoolEvents)
                        {
                            ContextThreadManager.Instance.TPoolEvents.Enqueue(PEvent);
                            ContextThreadManager.Instance.mTPoolTrigger.Set();
                        }
                    }
                    break;
                case EAsyncType.Delay:
                    {
                        System.Threading.ThreadPool.QueueUserWorkItem((data) =>
                        {
                            PEvent.PostAction();
                            lock (PEvent.ContinueThread.ContinueEvents)
                            {
                                PEvent.ContinueThread.ContinueEvents.Enqueue(PEvent);
                            }
                        });
                    }
                    break;
            }
        }
        public bool IsCompleted
        {
            get
            {
                return FinishedContinue;
                //return task.IsCompleted;
            }
        }
        public object GetResult()
        {
            if (PEvent.ExceptionInfo != null)
            {
                //throw PEvent.ExceptionInfo;
                Profiler.Log.WriteException(PEvent.ExceptionInfo);
            }
            return Result;
        }
    }
    public static class TaskExtensionForPost
    {
        public static TaskAwaiter AwaitPost(this System.Threading.Tasks.Task task, PostEvent pe)
        {
            pe.Awaiter = new TaskAwaiter(task, pe);
            return pe.Awaiter;
        }
    }
}
