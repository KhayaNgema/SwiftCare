using Microsoft.EntityFrameworkCore;
using SwiftCare.Models.VolunteerOnboarding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SwiftCare.Data
{
    public static class VolunteerPolicySeedData
    {
        public static async Task SeedAsync(
            SwiftCareDbContext context,
            CancellationToken cancellationToken = default)
        {
            var existingPolicies =
                await context.VolunteerPolicies
                    .ToListAsync(cancellationToken);

            var now = DateTime.Now;

            // =========================================================
            // 1. VOLUNTEER CODE OF CONDUCT
            // =========================================================

            await AddPolicyIfMissingAsync(
                context,
                existingPolicies,
                new VolunteerPolicy
                {
                    Title = "Volunteer Code of Conduct",
                    Description =
                        @"1. PURPOSE
This policy establishes the standards of behaviour expected from every SwiftCare volunteer. Volunteers represent SwiftCare whenever they participate in volunteer activities and must conduct themselves in a professional, respectful and responsible manner.

2. RESPECT AND DIGNITY
Volunteers must treat residents, families, visitors, employees, contractors and other volunteers with dignity, patience, courtesy and respect. Discrimination, intimidation, bullying, harassment, humiliation, threatening behaviour or abusive language will not be tolerated.

3. PROFESSIONAL BEHAVIOUR
Volunteers must follow reasonable instructions from authorised SwiftCare employees and remain within the responsibilities of their assigned role. Volunteers must not perform clinical, medication-related or other restricted activities unless specifically authorised and appropriately qualified.

4. HONESTY AND INTEGRITY
Volunteers must provide accurate information to SwiftCare and must immediately report mistakes, incidents or concerns. Volunteers must not falsify records, timesheets, documents or other information.

5. CONFLICTS OF INTEREST
Volunteers must disclose relationships, financial interests or other circumstances that could create a conflict of interest or affect their impartiality.

6. SAFETY
Volunteers must follow SwiftCare safety procedures and immediately report hazards, injuries, suspected abuse, safeguarding concerns or other risks.

7. COMPLIANCE
Failure to comply with this Code may result in corrective action, suspension or termination of the volunteer relationship.",
                    Version = "1.0",
                    IsRequired = true,
                    IsActive = true,
                    PublishedDate = now,
                    CreatedDate = now,
                    ModifiedDate = now
                },
                cancellationToken);

            // =========================================================
            // 2. RESIDENT RIGHTS, DIGNITY AND SAFEGUARDING
            // =========================================================

            await AddPolicyIfMissingAsync(
                context,
                existingPolicies,
                new VolunteerPolicy
                {
                    Title = "Resident Rights, Dignity and Safeguarding",
                    Description =
                        @"1. PURPOSE
SwiftCare is committed to protecting the safety, dignity, privacy and wellbeing of every resident. Volunteers have a responsibility to support residents respectfully and to report concerns immediately.

2. RESIDENT RIGHTS
Residents must be treated as individuals with their own preferences, beliefs, choices and personal boundaries. Volunteers must respect a resident's right to make decisions where appropriate and must never pressure a resident into participating in an activity.

3. DIGNITY AND RESPECT
Volunteers must communicate respectfully and must not ridicule, shame, threaten, ignore or deliberately embarrass a resident. Assistance must be provided in a manner that protects dignity and privacy.

4. SAFEGUARDING
Volunteers must remain alert to possible physical, emotional, psychological, sexual or financial abuse, neglect, exploitation or inappropriate treatment. Concerns must be reported immediately to an authorised SwiftCare employee.

5. FINANCIAL AND PERSONAL PROPERTY
Volunteers must never borrow, request, accept or take money, bank cards, valuables, medication or personal property from residents unless a formally authorised SwiftCare process specifically permits it.

6. PHYSICAL CONTACT
Volunteers must respect personal boundaries. Physical contact must be appropriate, necessary, culturally respectful and welcomed by the resident.

7. REPORTING
Volunteers must never investigate suspected abuse themselves. They must report concerns promptly to the appropriate SwiftCare supervisor or safeguarding representative.

The safety and wellbeing of residents takes priority over convenience, personal relationships or informal arrangements.",
                    Version = "1.0",
                    IsRequired = true,
                    IsActive = true,
                    PublishedDate = now,
                    CreatedDate = now,
                    ModifiedDate = now
                },
                cancellationToken);

            // =========================================================
            // 3. CONFIDENTIALITY, PRIVACY AND INFORMATION SECURITY
            // =========================================================

            await AddPolicyIfMissingAsync(
                context,
                existingPolicies,
                new VolunteerPolicy
                {
                    Title = "Confidentiality, Privacy and Information Security",
                    Description =
                        @"1. PURPOSE
Volunteers may encounter confidential information relating to residents, families, employees, volunteers and SwiftCare operations. Such information must be protected at all times.

2. CONFIDENTIAL INFORMATION
Confidential information may include medical information, personal details, contact information, family circumstances, photographs, care information, passwords, records, financial information and internal SwiftCare business information.

3. ACCESS
Volunteers may only access information that is necessary for their authorised responsibilities. Volunteers must never attempt to access records, systems, files or accounts outside their assigned responsibilities.

4. DISCLOSURE
Confidential information must not be discussed with friends, family members, other residents, unauthorised employees or members of the public. Information must not be shared through email, messaging applications, social media or other platforms unless authorised.

5. PASSWORDS AND SYSTEM ACCESS
Volunteers must keep passwords confidential, must not share accounts and must log out of SwiftCare systems when finished. Suspected account compromise must be reported immediately.

6. RECORDS AND DEVICES
Volunteers must not copy, download, photograph or remove confidential records unless specifically authorised. SwiftCare information must not be stored on personal devices without approval.

7. BREACHES
Any accidental or intentional disclosure, loss of information, suspicious access or security incident must be reported immediately.

Confidentiality obligations continue after the volunteer relationship with SwiftCare ends.",
                    Version = "1.0",
                    IsRequired = true,
                    IsActive = true,
                    PublishedDate = now,
                    CreatedDate = now,
                    ModifiedDate = now
                },
                cancellationToken);

            // =========================================================
            // 4. HEALTH, SAFETY AND INFECTION PREVENTION
            // =========================================================

            await AddPolicyIfMissingAsync(
                context,
                existingPolicies,
                new VolunteerPolicy
                {
                    Title = "Health, Safety and Infection Prevention",
                    Description =
                        @"1. PURPOSE
SwiftCare is committed to maintaining a safe environment for residents, employees, volunteers and visitors. Volunteers must comply with all applicable SwiftCare health, safety and infection-prevention procedures.

2. PERSONAL HYGIENE
Volunteers must maintain appropriate personal hygiene and follow required hand-washing and hand-sanitising procedures. Volunteers must follow instructions relating to protective clothing and personal protective equipment.

3. ILLNESS
Volunteers must inform their supervisor if they are unwell or believe they may have an infectious condition that could place vulnerable residents at risk. Volunteers must follow SwiftCare instructions before returning to volunteer activities.

4. HAZARDS AND INCIDENTS
Spills, damaged equipment, unsafe conditions, injuries, accidents, exposure incidents and other hazards must be reported immediately. Volunteers must not attempt repairs or interventions outside their authority.

5. INFECTION PREVENTION
Volunteers must follow applicable infection-control instructions and must not bring unnecessary personal items into restricted areas.

6. EMERGENCIES
Volunteers must follow emergency procedures and instructions from authorised staff. Volunteers must not place themselves or residents at additional risk by attempting tasks for which they have not been trained.

7. RESTRICTED ACTIVITIES
Volunteers must not administer medication, perform clinical procedures, operate restricted equipment or provide medical advice unless expressly authorised and appropriately qualified.

Safety instructions issued by authorised SwiftCare personnel must be followed at all times.",
                    Version = "1.0",
                    IsRequired = true,
                    IsActive = true,
                    PublishedDate = now,
                    CreatedDate = now,
                    ModifiedDate = now
                },
                cancellationToken);

            // =========================================================
            // 5. PROFESSIONAL BOUNDARIES
            // =========================================================

            await AddPolicyIfMissingAsync(
                context,
                existingPolicies,
                new VolunteerPolicy
                {
                    Title = "Professional Boundaries and Appropriate Relationships",
                    Description =
                        @"1. PURPOSE
Volunteers often develop positive relationships with residents. These relationships must remain appropriate, respectful and consistent with the volunteer's role.

2. PROFESSIONAL BOUNDARIES
Volunteers must maintain appropriate boundaries and must not use their position to obtain personal, financial, romantic or other inappropriate benefits.

3. GIFTS AND MONEY
Volunteers must not request or accept money, loans, expensive gifts or personal financial benefits from residents or their families. Any offered gift that could create a conflict or appearance of favouritism must be discussed with a supervisor.

4. PERSONAL CONTACT
Volunteers must not establish inappropriate private relationships with residents or use personal communication channels to bypass SwiftCare procedures.

5. RESIDENT DEPENDENCY
Volunteers must avoid creating unnecessary emotional or practical dependency. Volunteers should encourage residents to use appropriate SwiftCare support and staff resources.

6. PERSONAL BELIEFS
Volunteers must respect the beliefs, culture, identity and personal choices of residents and must never pressure residents to adopt the volunteer's religious, political or personal beliefs.

7. PHYSICAL AND EMOTIONAL BOUNDARIES
Any physical contact must be appropriate and welcomed. Volunteers must not engage in sexual, romantic, threatening or otherwise inappropriate conduct.

8. REPORTING
Volunteers should discuss boundary concerns with their supervisor before a situation escalates.

When uncertain whether conduct is appropriate, volunteers must seek guidance from an authorised SwiftCare employee.",
                    Version = "1.0",
                    IsRequired = true,
                    IsActive = true,
                    PublishedDate = now,
                    CreatedDate = now,
                    ModifiedDate = now
                },
                cancellationToken);

            // =========================================================
            // 6. ATTENDANCE, RESPONSIBILITIES AND COMMUNICATION
            // =========================================================

            await AddPolicyIfMissingAsync(
                context,
                existingPolicies,
                new VolunteerPolicy
                {
                    Title = "Volunteer Attendance, Responsibilities and Communication",
                    Description =
                        @"1. COMMITMENT
Volunteers are expected to honour the days, times and responsibilities agreed with SwiftCare. Reliable attendance is important because residents and staff may depend on volunteer support.

2. ARRIVAL
Volunteers should arrive on time, follow sign-in procedures where applicable and report to their designated supervisor or responsible employee.

3. ABSENCE
If a volunteer cannot attend an agreed shift or activity, they must notify the appropriate SwiftCare contact as early as reasonably possible.

4. RESPONSIBILITIES
Volunteers must complete assigned activities responsibly and must ask for clarification when they do not understand an instruction. Volunteers must not accept responsibilities outside their approved role without authorisation.

5. COMMUNICATION
Communication with SwiftCare staff must remain respectful and professional. Concerns, difficulties, incidents and changes in availability should be communicated promptly.

6. DRESS AND IDENTIFICATION
Volunteers must comply with applicable SwiftCare dress, identification and appearance requirements for their assigned activities.

7. PERSONAL ACTIVITIES
Volunteers must not use volunteer time, SwiftCare facilities or resident interactions for unauthorised personal business.

8. SUPERVISION
Volunteers must follow the direction of their designated supervisor and should report unresolved concerns through the appropriate SwiftCare management structure.

9. ENDING A SHIFT
Volunteers should complete required handover or reporting procedures before leaving.

Repeated unexplained absence, failure to communicate or refusal to perform reasonable authorised responsibilities may result in review of the volunteer placement.",
                    Version = "1.0",
                    IsRequired = true,
                    IsActive = true,
                    PublishedDate = now,
                    CreatedDate = now,
                    ModifiedDate = now
                },
                cancellationToken);

            // =========================================================
            // 7. SOCIAL MEDIA, PHOTOGRAPHY AND PUBLIC COMMUNICATION
            // =========================================================

            await AddPolicyIfMissingAsync(
                context,
                existingPolicies,
                new VolunteerPolicy
                {
                    Title = "Social Media, Photography and Public Communications",
                    Description =
                        @"1. PURPOSE
Volunteers must protect resident privacy and the reputation of SwiftCare when using social media, messaging services or other public communication channels.

2. PHOTOGRAPHS AND RECORDINGS
Volunteers must not photograph, video or audio-record residents, employees, visitors, documents, care activities or restricted areas unless expressly authorised under an approved SwiftCare process.

3. SOCIAL MEDIA
Volunteers must not publish confidential information or identifying details about residents, families, staff or SwiftCare operations. This includes names, photographs, medical information, locations, conversations and personal stories.

4. RESIDENT CONSENT
A resident's willingness to appear in a photograph or social-media post does not automatically authorise publication. Volunteers must follow SwiftCare's approved consent and communication procedures.

5. OFFICIAL COMMUNICATION
Volunteers must not represent themselves as an official spokesperson for SwiftCare or make statements on behalf of SwiftCare unless specifically authorised.

6. ONLINE CONDUCT
Volunteers must not use social media to harass, threaten, ridicule or disclose confidential information about residents, employees or other volunteers.

7. INCIDENTS
If confidential information is accidentally posted, sent or disclosed, the volunteer must report the incident immediately and must not attempt to conceal it.

8. CONTINUING RESPONSIBILITY
These requirements continue to apply after a volunteer's placement ends where the information concerned remains confidential.

When in doubt, volunteers must obtain permission from an authorised SwiftCare representative before publishing or sharing information related to SwiftCare.",
                    Version = "1.0",
                    IsRequired = true,
                    IsActive = true,
                    PublishedDate = now,
                    CreatedDate = now,
                    ModifiedDate = now
                },
                cancellationToken);

            // =========================================================
            // SAVE
            // =========================================================

            await context.SaveChangesAsync(
                cancellationToken);
        }

        private static async Task AddPolicyIfMissingAsync(
            SwiftCareDbContext context,
            List<VolunteerPolicy> existingPolicies,
            VolunteerPolicy policy,
            CancellationToken cancellationToken)
        {
            var exists =
                existingPolicies.Any(x =>
                    x.Title == policy.Title &&
                    x.Version == policy.Version);

            if (exists)
                return;

            context.VolunteerPolicies.Add(policy);

            existingPolicies.Add(policy);

            await Task.CompletedTask;
        }
    }
}