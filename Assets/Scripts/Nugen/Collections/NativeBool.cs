using UnityEngine;

using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

using Unity.Jobs;
using Unity.Burst;

using System.Threading;
using System.Runtime.InteropServices;
using System;

namespace nugen.collections
{

    [StructLayout(LayoutKind.Sequential), NativeContainer, NativeContainerSupportsDeallocateOnJobCompletion]
    unsafe public struct NativeBool : IDisposable
    { 

        [NativeDisableUnsafePtrRestriction]
        int* m_Counter;

        public int Count
        {

            get
            {

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif

                return *m_Counter;

            }

            set
            {

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif

                *m_Counter = value;

            }

        }

        public bool Value
        {

            get
            {

                return Count > 0;

            }

            set
            {

                Count = 0;

            }

        }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        AtomicSafetyHandle m_Safety;

        [NativeSetClassTypeToNullOnSchedule]
        DisposeSentinel m_DisposeSentinel;
#endif

        Allocator m_AllocatorLabel;

        public NativeBool(Allocator allocator)
        {

            m_AllocatorLabel = allocator;

            m_Counter = (int*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<int>(), 4, allocator);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            DisposeSentinel.Create(out m_Safety, out m_DisposeSentinel, 0, allocator);
#endif

            Count = 0;

        }

        public void Add(int val)
        {

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif

            (*m_Counter)+=val;

        }

        public bool IsCreated
        {
            get { return m_Counter != null; }
        }

        public void Dispose()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            DisposeSentinel.Dispose(ref m_Safety, ref m_DisposeSentinel);
#endif

            UnsafeUtility.Free(m_Counter, m_AllocatorLabel);
            m_Counter = null;
        }

        [NativeContainer]
        internal unsafe struct NativeBoolDispose
        {
            [NativeDisableUnsafePtrRestriction]
            internal int* m_Counter;
            internal Allocator m_AllocatorLabel;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            internal AtomicSafetyHandle m_Safety;
#endif

            public void Dispose()
            {
                UnsafeUtility.Free(m_Counter, m_AllocatorLabel);
            }
        }

        [BurstCompile]
        internal struct NativeBoolDisposeJob : IJob
        {
            internal NativeBoolDispose Data;

            public void Execute()
            {
                Data.Dispose();
            }

        }

        public JobHandle Dispose(JobHandle inputDeps)
        {
            if (m_AllocatorLabel == Allocator.Invalid)
            {
                throw new InvalidOperationException("The NativeBool can not be Disposed because it was not allocated with a valid allocator.");
            }

            if (m_Counter == null)
            {
                throw new InvalidOperationException("The NativeBool is already disposed.");
            }

            if (m_AllocatorLabel > Allocator.None)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                DisposeSentinel.Clear(ref m_DisposeSentinel);
#endif


                var jobHandle = new NativeBoolDisposeJob { Data = new NativeBoolDispose { m_Counter = m_Counter, m_AllocatorLabel = m_AllocatorLabel,
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    m_Safety = m_Safety
#endif

                } }.Schedule(inputDeps);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.Release(m_Safety);
#endif

                m_Counter = null;
                m_AllocatorLabel = Allocator.Invalid;

                return jobHandle;
            }

            m_Counter = null;

            return inputDeps;
        }

        [NativeContainer, NativeContainerIsAtomicWriteOnly]
        unsafe public struct Concurrent
        {

            [NativeDisableUnsafePtrRestriction]
            int* m_Counter;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle m_Safety;
#endif

            public static implicit operator NativeBool.Concurrent (NativeBool nbool)
            {

                NativeBool.Concurrent concurrent;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndThrow(nbool.m_Safety);
                concurrent.m_Safety = nbool.m_Safety;
                AtomicSafetyHandle.UseSecondaryVersion(ref concurrent.m_Safety);
#endif

                concurrent.m_Counter = nbool.m_Counter;
                return concurrent;

            }

            public void Add(int val)
            {

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif

                Interlocked.Add(ref *m_Counter, val);

            }

        }

    }

}
