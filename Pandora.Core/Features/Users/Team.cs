using System;
using System.Collections.Generic;

namespace Pandora.Core.Features.Users
{
    public class Team
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public List<Guid> UserIds { get; set; } = new();
    }
}
