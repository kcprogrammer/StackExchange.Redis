﻿using System;
using Xunit;
using Xunit.Abstractions;

namespace StackExchange.Redis.Tests
{
    public class Expiry : TestBase
    {
        public Expiry(ITestOutputHelper output) : base (output) { }

        private static string[] GetMap(bool disablePTimes)
        {
            if (disablePTimes)
            {
                return new[] { "pexpire", "pexpireat", "pttl" };
            }
            return null;
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestBasicExpiryTimeSpan(bool disablePTimes)
        {
            using (var muxer = Create(disabledCommands: GetMap(disablePTimes)))
            {
                RedisKey key = Me();
                var conn = muxer.GetDatabase();
                conn.KeyDelete(key, CommandFlags.FireAndForget);

                conn.StringSet(key, "new value", flags: CommandFlags.FireAndForget);
                var a = conn.KeyTimeToLiveAsync(key);
                conn.KeyExpire(key, TimeSpan.FromHours(1), CommandFlags.FireAndForget);
                var b = conn.KeyTimeToLiveAsync(key);
                conn.KeyExpire(key, (TimeSpan?)null, CommandFlags.FireAndForget);
                var c = conn.KeyTimeToLiveAsync(key);
                conn.KeyExpire(key, TimeSpan.FromHours(1.5), CommandFlags.FireAndForget);
                var d = conn.KeyTimeToLiveAsync(key);
                conn.KeyExpire(key, TimeSpan.MaxValue, CommandFlags.FireAndForget);
                var e = conn.KeyTimeToLiveAsync(key);

                Assert.Null(muxer.Wait(a));
                var time = muxer.Wait(b);
                Assert.NotNull(time);
                Assert.True(time > TimeSpan.FromMinutes(59.9) && time <= TimeSpan.FromMinutes(60));
                Assert.Null(muxer.Wait(c));
                time = muxer.Wait(d);
                Assert.NotNull(time);
                Assert.True(time > TimeSpan.FromMinutes(89.9) && time <= TimeSpan.FromMinutes(90));
                Assert.Null(muxer.Wait(e));
            }
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(false, false)]
        public void TestBasicExpiryDateTime(bool disablePTimes, bool utc)
        {
            using (var muxer = Create(disabledCommands: GetMap(disablePTimes)))
            {
                RedisKey key = Me();
                var conn = muxer.GetDatabase();
                conn.KeyDelete(key, CommandFlags.FireAndForget);

                var now = utc ? DateTime.UtcNow : new DateTime(DateTime.UtcNow.Ticks + TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time").BaseUtcOffset.Ticks, DateTimeKind.Local);
                conn.StringSet(key, "new value", flags: CommandFlags.FireAndForget);
                var a = conn.KeyTimeToLiveAsync(key);
                conn.KeyExpire(key, now.AddHours(1), CommandFlags.FireAndForget);
                var b = conn.KeyTimeToLiveAsync(key);
                conn.KeyExpire(key, (DateTime?)null, CommandFlags.FireAndForget);
                var c = conn.KeyTimeToLiveAsync(key);
                conn.KeyExpire(key, now.AddHours(1.5), CommandFlags.FireAndForget);
                var d = conn.KeyTimeToLiveAsync(key);
                conn.KeyExpire(key, DateTime.MaxValue, CommandFlags.FireAndForget);
                var e = conn.KeyTimeToLiveAsync(key);

                Assert.Null(muxer.Wait(a));
                var time = muxer.Wait(b);
                Assert.NotNull(time);
                Output.WriteLine(time?.ToString());
                Assert.True(time > TimeSpan.FromMinutes(59.9) && time <= TimeSpan.FromMinutes(60));
                Assert.Null(muxer.Wait(c));
                time = muxer.Wait(d);
                Assert.NotNull(time);
                Assert.True(time > TimeSpan.FromMinutes(89.9) && time <= TimeSpan.FromMinutes(90));
                Assert.Null(muxer.Wait(e));
            }
        }
    }
}