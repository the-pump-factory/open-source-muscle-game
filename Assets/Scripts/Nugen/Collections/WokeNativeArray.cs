using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;

using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs.LowLevel.Unsafe;

using System;
using System.Threading;
using System.Runtime.InteropServices;

namespace nugen.collections
{

    [StructLayout(LayoutKind.Sequential), NativeContainer, NativeContainerSupportsDeallocateOnJobCompletion]
    unsafe public struct WokeNativeArray<T> : IDisposable where T : struct
    {

        public const int IntsPerCacheLine = JobsUtility.CacheLineSize / sizeof(int);

        [NativeDisableUnsafePtrRestriction] 
        int* m_Counter;

        public int Counter
        {

            get
            {

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                //AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                int count = 0;
                for (int i = 0; i < JobsUtility.MaxJobThreadCount; ++i)
                    count += m_Counter[IntsPerCacheLine * i];
                return count;

            }

            set
            {

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                //AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
                for (int i = 1; i < JobsUtility.MaxJobThreadCount; ++i)
                    m_Counter[IntsPerCacheLine * i] = 0;
                *m_Counter = value;

            }

        }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        AtomicSafetyHandle m_Safety;

        [NativeSetClassTypeToNullOnSchedule]
        DisposeSentinel m_DisposeSentinel;
#endif

        Allocator m_AllocatorLabel;

        public void Add(int val)
        {

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            //AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif

            (*m_Counter) += val;

        }

        public bool IsCreated
        {
            get { return m_Counter != null; }
        }

        public bool CheckForUpdate(bool reset = true)
        {

            int count = Counter;

            if (reset) Add(-count);

            return count > 0;

        }

        public void Reset()
        {

            Add(-Counter);

        }

        [NativeDisableParallelForRestriction, NativeDisableContainerSafetyRestriction]
        private NativeArray<T> array;

        public NativeArray<T> Array
        {

            get
            {

                return array;

            }

        }

        public int Length
        {

            get
            {

                return array.Length;

            }

        }

        public T this[int i]
        {

            get
            {

                return array[i];

            }

            set
            {

                Add(1);

                array[i] = value;

            }

        }

        public void Dispose()
        {

            if (array.IsCreated) array.Dispose();

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            DisposeSentinel.Dispose(ref m_Safety, ref m_DisposeSentinel);
#endif

            UnsafeUtility.Free(m_Counter, m_AllocatorLabel);
            m_Counter = null;

        }

        [NativeContainer]
        internal unsafe struct WokeNativeArrayDispose
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
        internal struct WokeNativeArrayDisposeJob : IJob
        {
            internal WokeNativeArrayDispose Data;

            public void Execute()
            {
                Data.Dispose();
            }

        }

        public JobHandle Dispose(JobHandle inputDeps)
        {

            if (array.IsCreated) inputDeps = array.Dispose(inputDeps);

            if (m_AllocatorLabel == Allocator.Invalid)
            {
                throw new InvalidOperationException("The WokeNativeArray can not be Disposed because it was not allocated with a valid allocator.");
            }

            if (m_Counter == null)
            {
                throw new InvalidOperationException("The WokeNativeArray is already disposed.");
            }

            if (m_AllocatorLabel > Allocator.None)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                DisposeSentinel.Clear(ref m_DisposeSentinel);
#endif

                var jobHandle = new WokeNativeArrayDisposeJob { Data = new WokeNativeArrayDispose { m_Counter = m_Counter, m_AllocatorLabel = m_AllocatorLabel,

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

        public WokeNativeArray(T[] referenceArray, Allocator allocator)
        {

            array = new NativeArray<T>(referenceArray, allocator);

            m_AllocatorLabel = allocator;

            m_Counter = (int*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<int>() * IntsPerCacheLine * JobsUtility.MaxJobThreadCount, 4, allocator);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            DisposeSentinel.Create(out m_Safety, out m_DisposeSentinel, 0, allocator);
#endif

            Counter = 0;

        }

        public WokeNativeArray(NativeArray<T> referenceArray, Allocator allocator)
        {

            array = new NativeArray<T>(referenceArray, allocator);

            m_AllocatorLabel = allocator;

            m_Counter = (int*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<int>() * IntsPerCacheLine * JobsUtility.MaxJobThreadCount, 4, allocator);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            DisposeSentinel.Create(out m_Safety, out m_DisposeSentinel, 0, allocator);
#endif

            Counter = 0;

        }

        public WokeNativeArray(int length, Allocator allocator, NativeArrayOptions options = NativeArrayOptions.ClearMemory)
        {

            array = new NativeArray<T>(length, allocator, options);

            m_AllocatorLabel = allocator;

            m_Counter = (int*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<int>() * IntsPerCacheLine * JobsUtility.MaxJobThreadCount, 4, allocator);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            DisposeSentinel.Create(out m_Safety, out m_DisposeSentinel, 0, allocator);
#endif

            Counter = 0;

        }

        [NativeContainer, NativeContainerIsAtomicWriteOnly]
        unsafe public struct Concurrent
        {

            [NativeDisableParallelForRestriction, NativeDisableContainerSafetyRestriction]
            private NativeArray<T> array;

            [NativeDisableUnsafePtrRestriction]
            int* m_Counter;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle m_Safety;
#endif

            [NativeSetThreadIndex]
            int m_ThreadIndex;

            public static implicit operator WokeNativeArray<T>.Concurrent(WokeNativeArray<T> wna)
            {

                WokeNativeArray<T>.Concurrent concurrent;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                //AtomicSafetyHandle.CheckWriteAndThrow(wna.m_Safety);
                concurrent.m_Safety = wna.m_Safety;
                AtomicSafetyHandle.UseSecondaryVersion(ref concurrent.m_Safety);
#endif

                concurrent.array = wna.array;

                concurrent.m_Counter = wna.m_Counter;

                concurrent.m_ThreadIndex = 0;

                return concurrent;

            }

            public void Add(int val)
            {

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                //AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif

                //Interlocked.Add(ref *m_Counter, val);

                m_Counter[IntsPerCacheLine * m_ThreadIndex] += val; 

            }

            public int Length
            {

                get
                {

                    return array.Length;

                }

            }

            public T this[int i]
            {

                get
                {

                    return array[i];

                }

                set
                {

                    Add(1);

                    array[i] = value;

                }

            }

        }

    }

}
