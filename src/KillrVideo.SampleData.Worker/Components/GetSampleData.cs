﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cassandra;
using KillrVideo.Utils;

namespace KillrVideo.SampleData.Worker.Components
{
    /// <summary>
    /// Gets sample data from Cassandra.
    /// </summary>
    public class GetSampleData : IGetSampleData
    {
        private readonly ISession _session;
        private readonly TaskCache<string, PreparedStatement> _statementCache;

        public GetSampleData(ISession session, TaskCache<string, PreparedStatement> statementCache)
        {
            if (session == null) throw new ArgumentNullException("session");
            if (statementCache == null) throw new ArgumentNullException("statementCache");
            _session = session;
            _statementCache = statementCache;
        }

        /// <summary>
        /// Gets a random collection of sample user ids.  May include duplicate user ids.
        /// </summary>
        public async Task<List<Guid>> GetRandomSampleUserIds(int count)
        {
            // Use the token() function to get the first count ids above an below a random user Id
            PreparedStatement[] prepared = await _statementCache.NoContext.GetOrAddAllAsync(
                "SELECT userid FROM sample_data_users WHERE token(userid) >= token(?) LIMIT ?",
                "SELECT userid FROM sample_data_users WHERE token(userid) < token(?) LIMIT ?");

            var randomId = Guid.NewGuid();
            var execTasks = new Task<RowSet>[2];

            // Execute in parallel
            execTasks[0] = _session.ExecuteAsync(prepared[0].Bind(randomId, count));
            execTasks[1] = _session.ExecuteAsync(prepared[1].Bind(randomId, count));

            RowSet[] results = await Task.WhenAll(execTasks).ConfigureAwait(false);
            
            // Union the results together and take the first count available
            var userIds = results.SelectMany(rowset => rowset).Select(r => r.GetValue<Guid>("userid")).Take(count).ToList();
            if (userIds.Count == 0)
                throw new InvalidOperationException("There are currently no sample users available.");

            // If we got enough users, just return
            if (userIds.Count == count)
                return userIds;

            // We didn't get enough, so just fill the remaining we need by repeating the ones we did get
            int i = 0;
            while (userIds.Count < count)
            {
                userIds.Add(userIds[i]);
                i++;
            }
            return userIds;
        }

        /// <summary>
        /// Gets a random collection of video ids for videos on the site.  May include duplicate video ids.
        /// </summary>
        public async Task<List<Guid>> GetRandomVideoIds(int count)
        {
            // Use the token() function to get the first count ids above an below a random video Id
            PreparedStatement[] prepared = await _statementCache.NoContext.GetOrAddAllAsync(
                "SELECT videoid FROM sample_data_videos WHERE token(videoid) >= token(?) LIMIT ?",
                "SELECT videoid FROM sample_data_videos WHERE token(videoid) < token(?) LIMIT ?");

            var randomId = Guid.NewGuid();
            var execTasks = new Task<RowSet>[2];

            // Execute in parallel
            execTasks[0] = _session.ExecuteAsync(prepared[0].Bind(randomId, count));
            execTasks[1] = _session.ExecuteAsync(prepared[1].Bind(randomId, count));

            RowSet[] results = await Task.WhenAll(execTasks).ConfigureAwait(false);

            // Union the results together and take the first count available
            var videoIds = results.SelectMany(rowset => rowset).Select(r => r.GetValue<Guid>("videoid")).Take(count).ToList();
            if (videoIds.Count == 0)
                throw new InvalidOperationException("There are currently no videos available.");

            // If we got enough videos, just return
            if (videoIds.Count == count)
                return videoIds;

            // We didn't get enough, so just fill the remaining we need by repeating the ones we did get
            int i = 0;
            while (videoIds.Count < count)
            {
                videoIds.Add(videoIds[i]);
                i++;
            }
            return videoIds;
        }
    }
}