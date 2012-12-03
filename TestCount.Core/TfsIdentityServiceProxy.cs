using Microsoft.TeamFoundation.Framework.Client;
using Microsoft.TeamFoundation.Framework.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TestCount.Core
{
    public class TfsIdentityServiceProxy : IIdentityServiceProxy
    {
        private readonly IIdentityManagementService identityService;
        private readonly Dictionary<string, UserIdentity> users;

        public string RootGroupName
        {
            get;
            set;
        }

        public TfsIdentityServiceProxy(IIdentityManagementService identityService)
        {
            this.identityService = identityService;
            this.users = new Dictionary<string, UserIdentity>();
            this.RootGroupName = @"[WPF_Scrum]\Contributors";
        }

        public IEnumerable<UserIdentity> GetUserIdentities()
        {
            this.ClearLocalState();

            var rootIdentity = this.identityService.ReadIdentity(IdentitySearchFactor.AccountName, this.RootGroupName, MembershipQuery.Direct, ReadIdentityOptions.None);
            var identities = this.identityService.ReadIdentities(rootIdentity.Members, MembershipQuery.Direct, ReadIdentityOptions.None);

            foreach (TeamFoundationIdentity member in identities)
            {
                if(member.IsContainer)
                {
                    this.AddUsersInGroup(member);
                }
                else
                {
                    this.AddUserWithNoGroup(member);
                }
            }

            return this.users.Values.ToList();
        }
  
        private void ClearLocalState()
        {
            this.users.Clear();
        }

        private void AddUserWithNoGroup(TeamFoundationIdentity member)
        {
            var user = this.GetOrCreateUserIdentity(member);
            this.users[user.IdentityKey] = user;
        }
  
        private void AddUsersInGroup(TeamFoundationIdentity groupMember)
        {
            var groupMembers = this.identityService.ReadIdentities(groupMember.Members, MembershipQuery.Direct, ReadIdentityOptions.None);
            var groupIdentity = this.GetGroupIdentity(groupMember);

            foreach (TeamFoundationIdentity member in groupMembers)
            {
                if (member.IsContainer)
                {
                    // multi-level groups not supported
                    continue;
                }

                var user = this.GetOrCreateUserIdentity(member);
                user.Groups.Add(groupIdentity);

                this.users[user.IdentityKey] = user;
            }
        }

        private GroupIdentity GetGroupIdentity(TeamFoundationIdentity groupMember)
        {
            var newGroupIdentity = new GroupIdentity();
            newGroupIdentity.DisplayName = groupMember.DisplayName;
            newGroupIdentity.IdentityKey = groupMember.UniqueName;

            return newGroupIdentity;
        }

        private UserIdentity GetOrCreateUserIdentity(TeamFoundationIdentity tfsIdentity)
        {
            if (this.users.ContainsKey(tfsIdentity.UniqueName))
            {
                return this.users[tfsIdentity.UniqueName];
            }
            else
            {
                var userIdentity = new UserIdentity()
                {
                    DisplayName = tfsIdentity.DisplayName,
                    IdentityKey = tfsIdentity.UniqueName,
                };

                return userIdentity;
            }
        }
    }
}
