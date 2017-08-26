﻿using Xunit;
using Xunit.Abstractions;

namespace StackExchange.Redis.Tests
{
    public class Databases : TestBase
    {
        public Databases(ITestOutputHelper output) : base (output) { }

        [Fact]
        public void CountKeys()
        {
            using (var muxer = Create(allowAdmin: true))
            {
                var server = GetServer(muxer);
                server.FlushDatabase(0, CommandFlags.FireAndForget);
                server.FlushDatabase(1, CommandFlags.FireAndForget);
            }
            using (var muxer = Create())
            {
                RedisKey key = Me();
                var db0 = muxer.GetDatabase(0);
                var db1 = muxer.GetDatabase(1);
                db0.StringSet("abc", "def", flags: CommandFlags.FireAndForget);
                db0.StringIncrement(key, flags: CommandFlags.FireAndForget);
                db1.StringIncrement(key, flags: CommandFlags.FireAndForget);

                var server = GetServer(muxer);
                var c0 = server.DatabaseSizeAsync(0);
                var c1 = server.DatabaseSizeAsync(1);

                Assert.Equal(2, muxer.Wait(c0));
                Assert.Equal(1, muxer.Wait(c1));
            }
        }

        [Fact]
        public void MultiDatabases()
        {
            using (var muxer = Create())
            {
                RedisKey key = Me();
                var db0 = muxer.GetDatabase(0);
                var db1 = muxer.GetDatabase(1);
                var db2 = muxer.GetDatabase(2);
                db0.Ping();

                db0.KeyDelete(key, CommandFlags.FireAndForget);
                db1.KeyDelete(key, CommandFlags.FireAndForget);
                db2.KeyDelete(key, CommandFlags.FireAndForget);

                muxer.WaitAll(
                    db0.StringSetAsync(key, "a"),
                    db1.StringSetAsync(key, "b"),
                    db2.StringSetAsync(key, "c")
                );

                var a = db0.StringGetAsync(key);
                var b = db1.StringGetAsync(key);
                var c = db2.StringGetAsync(key);
                muxer.WaitAll(a, b, c);

                Assert.Equal("a", muxer.Wait(a)); // db:0
                Assert.Equal("b", muxer.Wait(b)); // db:1
                Assert.Equal("c", muxer.Wait(c)); // db:2
            }
        }
    }
}
