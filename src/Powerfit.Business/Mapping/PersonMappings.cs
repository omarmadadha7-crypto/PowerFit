using DataAccess.Entities;
using Powerfit.Business.DTOs.Auth;

namespace Powerfit.Business.Mapping;

public static class PersonMappings
{
    public static AuthUserDto ToAuthUserDto(this Person person, int? userId, int? adminId, bool profileCompleted) =>
        new(person.PersonId,
            userId,
            adminId,
            person.FirstName,
            person.LastName,
            person.FullName,
            person.Email,
            person.Role,
            profileCompleted);
}
