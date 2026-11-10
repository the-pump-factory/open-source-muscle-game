using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;

using Unity.Burst;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;

using Unity.Collections.LowLevel.Unsafe;

namespace nugen.rigging
{

    [BurstCompile]
    public struct DriverJob : IJobParallelFor
    {

        public struct Constituent
        {

            public int startIndex, variablesIndexOffset, operationsIndexOffset;

            public int expressionCount;

            public float outX, outY, outZ, outW;

            public Constituent(int startIndex, int expressionCount, int variablesIndexOffset, int operationsIndexOffset)
            {

                this.startIndex = startIndex;
                this.operationsIndexOffset = operationsIndexOffset;
                this.variablesIndexOffset = variablesIndexOffset;
                this.expressionCount = expressionCount;

                outX = outY = outZ = outW = 0;

            }

        }

        public static Handler Create(List<DriverLogic.IDriver> drivers)
        {

            DriverJob job = new DriverJob()
            {

                variables = new NativeList<float>(drivers.Count, Allocator.TempJob),
                operations = new NativeList<DriverLogic.Operation>(drivers.Count, Allocator.TempJob),
                expressions = new NativeList<DriverLogic.Expression>(drivers.Count, Allocator.TempJob),
                constituents = new NativeList<Constituent>(drivers.Count, Allocator.TempJob)

            };

            return new Handler(job, drivers);

        }

        public class Handler : IDisposable
        {

            public DriverJob job;

            public List<DriverLogic.IDriver> drivers;

            public Handler(DriverJob job, List<DriverLogic.IDriver> drivers)
            {

                this.job = job;

                this.drivers = drivers;

            }

            public void Dispose()
            {

                if (job.variables.IsCreated) job.variables.Dispose();
                if (job.operations.IsCreated) job.operations.Dispose();
                if (job.expressions.IsCreated) job.expressions.Dispose();
                if (job.constituents.IsCreated) job.constituents.Dispose();

            }

            public JobHandle Schedule(JobHandle inputDeps, bool inject = true, int batchCount = 64)
            {

                if (inject) InjectDriverData();

                return job.Schedule(job.constituents.Length, batchCount, inputDeps);

            }

            public void InjectDriverData()
            {

                foreach (DriverLogic.IDriver driver in drivers) driver?.Queue(job.variables, job.operations, job.expressions, job.constituents);

            }

            public void UpdateDrivers()
            {

                for (int a = 0; a < drivers.Count; a++) drivers[a]?.Update(job.constituents[a]);

            }

            private JobHandle jobHandle;

            public void ScheduleLocal(JobHandle inputDeps, bool inject = true, int batchCount = 64)
            {

                jobHandle = Schedule(inputDeps, inject, batchCount);

            }

            public void AwaitCallback()
            {

                jobHandle.Complete();

                UpdateDrivers();

                Dispose();

            }

        }

        [ReadOnly]
        public NativeList<DriverLogic.Operation> operations;

        [ReadOnly]
        public NativeList<DriverLogic.Expression> expressions;

        [NativeDisableContainerSafetyRestriction]
        public NativeList<float> variables;

        [NativeDisableContainerSafetyRestriction]
        public NativeList<Constituent> constituents;

        public void Execute(int index)
        {

            Constituent driver = constituents[index];

            /*if (driver.expressionCount > 0) driver.outX = expressions[driver.startIndex].Evaluate(driver.variablesIndexOffset, variables);
            if (driver.expressionCount > 1) driver.outY = expressions[driver.startIndex + 1].Evaluate(driver.variablesIndexOffset, variables);
            if (driver.expressionCount > 2) driver.outZ = expressions[driver.startIndex + 2].Evaluate(driver.variablesIndexOffset, variables);
            if (driver.expressionCount > 3) driver.outW = expressions[driver.startIndex + 3].Evaluate(driver.variablesIndexOffset, variables);*/

            bool valid;

            valid = driver.expressionCount > 0;
            driver.outX = math.select(0, expressions[math.select(0, driver.startIndex, valid)].Evaluate(driver.variablesIndexOffset, driver.operationsIndexOffset, variables, operations), valid);

            valid = driver.expressionCount > 1;
            driver.outY = math.select(0, expressions[math.select(0, driver.startIndex + 1, valid)].Evaluate(driver.variablesIndexOffset, driver.operationsIndexOffset, variables, operations), valid);

            valid = driver.expressionCount > 2;
            driver.outZ = math.select(0, expressions[math.select(0, driver.startIndex + 2, valid)].Evaluate(driver.variablesIndexOffset, driver.operationsIndexOffset, variables, operations), valid);

            valid = driver.expressionCount > 3;
            driver.outW = math.select(0, expressions[math.select(0, driver.startIndex + 3, valid)].Evaluate(driver.variablesIndexOffset, driver.operationsIndexOffset, variables, operations), valid);

            constituents[index] = driver;

        }

    }

}
