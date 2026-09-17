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
            var now = DateTime.Now;

            // =========================================================
            // 1. VOLUNTEER CODE OF CONDUCT
            // =========================================================

            await SeedPolicyAsync(
                context,
                new VolunteerPolicy
                {
                    Title = "Volunteer Code of Conduct",
                    Version = "1.0",
                    IsRequired = true,
                    IsActive = true,
                    PublishedDate = now,
                    CreatedDate = now,
                    ModifiedDate = now,

                    Items = CreateItems(

                        CreateIntro(
                            "This policy establishes the standards of behaviour expected from every SwiftCare volunteer. Volunteers represent SwiftCare whenever they participate in volunteer activities and must conduct themselves in a professional, respectful and responsible manner."
                        ),

                        CreateItem(
                            "1.1 Respectful Behaviour",
                            "Volunteers must treat residents, families, visitors, employees, contractors and other volunteers with dignity, patience, courtesy and respect. Discrimination, intimidation, bullying, harassment, humiliation, threatening behaviour or abusive language will not be tolerated."
                        ),

                        CreateItem(
                            "1.2 Role Responsibilities",
                            "Volunteers must follow reasonable instructions from authorised SwiftCare employees and remain within the responsibilities of their assigned role. Volunteers must not perform clinical, medication-related or other restricted activities unless specifically authorised and appropriately qualified."
                        ),

                        CreateItem(
                            "1.3 Honesty and Integrity",
                            "Volunteers must provide accurate information to SwiftCare and must immediately report mistakes, incidents or concerns. Volunteers must not falsify records, timesheets, documents or other information."
                        ),

                        CreateItem(
                            "1.4 Conflicts of Interest",
                            "Volunteers must disclose relationships, financial interests or other circumstances that could create a conflict of interest or affect their impartiality."
                        ),

                        CreateItem(
                            "1.5 Safety and Reporting",
                            "Volunteers must follow SwiftCare safety procedures and immediately report hazards, injuries, suspected abuse, safeguarding concerns or other risks."
                        ),

                        CreateItem(
                            "1.6 Breach of Conduct",
                            "Failure to comply with this Code may result in corrective action, suspension or termination of the volunteer relationship."
                        )
                    )
                },
                cancellationToken);


            // =========================================================
            // 2. RESIDENT RIGHTS, DIGNITY AND SAFEGUARDING
            // =========================================================

            await SeedPolicyAsync(
                context,
                new VolunteerPolicy
                {
                    Title = "Resident Rights, Dignity and Safeguarding",
                    Version = "1.0",
                    IsRequired = true,
                    IsActive = true,
                    PublishedDate = now,
                    CreatedDate = now,
                    ModifiedDate = now,

                    Items = CreateItems(

                        CreateIntro(
                            "SwiftCare is committed to protecting the safety, dignity, privacy and wellbeing of every resident. Volunteers have a responsibility to support residents respectfully and to report concerns immediately."
                        ),

                        CreateItem(
                            "1.1 Respect for Residents",
                            "Residents must be treated as individuals with their own preferences, beliefs, choices and personal boundaries. Volunteers must respect a resident's right to make decisions where appropriate and must never pressure a resident into participating in an activity."
                        ),

                        CreateItem(
                            "1.2 Dignity and Privacy",
                            "Volunteers must communicate respectfully and must not ridicule, shame, threaten, ignore or deliberately embarrass a resident. Assistance must be provided in a manner that protects dignity and privacy."
                        ),

                        CreateItem(
                            "1.3 Protection from Abuse",
                            "Volunteers must remain alert to possible physical, emotional, psychological, sexual or financial abuse, neglect, exploitation or inappropriate treatment. Concerns must be reported immediately to an authorised SwiftCare employee."
                        ),

                        CreateItem(
                            "1.4 Personal Property",
                            "Volunteers must never borrow, request, accept or take money, bank cards, valuables, medication or personal property from residents unless a formally authorised SwiftCare process specifically permits it."
                        ),

                        CreateItem(
                            "1.5 Physical Boundaries",
                            "Volunteers must respect personal boundaries. Physical contact must be appropriate, necessary, culturally respectful and welcomed by the resident."
                        ),

                        CreateItem(
                            "1.6 Reporting Safeguarding Concerns",
                            "Volunteers must never investigate suspected abuse themselves. They must report concerns promptly to the appropriate SwiftCare supervisor or safeguarding representative."
                        ),

                        CreateItem(
                            "1.7 Resident Wellbeing",
                            "The safety and wellbeing of residents takes priority over convenience, personal relationships or informal arrangements."
                        )
                    )
                },
                cancellationToken);


            // =========================================================
            // 3. CONFIDENTIALITY, PRIVACY AND INFORMATION SECURITY
            // =========================================================

            await SeedPolicyAsync(
                context,
                new VolunteerPolicy
                {
                    Title = "Confidentiality, Privacy and Information Security",
                    Version = "1.0",
                    IsRequired = true,
                    IsActive = true,
                    PublishedDate = now,
                    CreatedDate = now,
                    ModifiedDate = now,

                    Items = CreateItems(

                        CreateIntro(
                            "Volunteers may encounter confidential information relating to residents, families, employees, volunteers and SwiftCare operations. Such information must be protected at all times."
                        ),

                        CreateItem(
                            "2.1 Confidential Information",
                            "Confidential information may include medical information, personal details, contact information, family circumstances, photographs, care information, passwords, records, financial information and internal SwiftCare business information."
                        ),

                        CreateItem(
                            "2.2 Authorised Access",
                            "Volunteers may only access information that is necessary for their authorised responsibilities. Volunteers must never attempt to access records, systems, files or accounts outside their assigned responsibilities."
                        ),

                        CreateItem(
                            "2.3 Information Sharing",
                            "Confidential information must not be discussed with friends, family members, other residents, unauthorised employees or members of the public. Information must not be shared through email, messaging applications, social media or other platforms unless authorised."
                        ),

                        CreateItem(
                            "2.4 Password and Account Security",
                            "Volunteers must keep passwords confidential, must not share accounts and must log out of SwiftCare systems when finished. Suspected account compromise must be reported immediately."
                        ),

                        CreateItem(
                            "2.5 Records and Personal Devices",
                            "Volunteers must not copy, download, photograph or remove confidential records unless specifically authorised. SwiftCare information must not be stored on personal devices without approval."
                        ),

                        CreateItem(
                            "2.6 Security Incidents",
                            "Any accidental or intentional disclosure, loss of information, suspicious access or security incident must be reported immediately."
                        ),

                        CreateItem(
                            "2.7 Continuing Confidentiality",
                            "Confidentiality obligations continue after the volunteer relationship with SwiftCare ends."
                        )
                    )
                },
                cancellationToken);


            // =========================================================
            // 4. HEALTH, SAFETY AND INFECTION PREVENTION
            // =========================================================

            await SeedPolicyAsync(
                context,
                new VolunteerPolicy
                {
                    Title = "Health, Safety and Infection Prevention",
                    Version = "1.0",
                    IsRequired = true,
                    IsActive = true,
                    PublishedDate = now,
                    CreatedDate = now,
                    ModifiedDate = now,

                    Items = CreateItems(

                        CreateIntro(
                            "SwiftCare is committed to maintaining a safe environment for residents, employees, volunteers and visitors. Volunteers must comply with all applicable SwiftCare health, safety and infection-prevention procedures."
                        ),

                        CreateItem(
                            "3.1 Personal Hygiene",
                            "Volunteers must maintain appropriate personal hygiene and follow required hand-washing and hand-sanitising procedures. Volunteers must follow instructions relating to protective clothing and personal protective equipment."
                        ),

                        CreateItem(
                            "3.2 Illness and Infection",
                            "Volunteers must inform their supervisor if they are unwell or believe they may have an infectious condition that could place vulnerable residents at risk. Volunteers must follow SwiftCare instructions before returning to volunteer activities."
                        ),

                        CreateItem(
                            "3.3 Hazards and Incidents",
                            "Spills, damaged equipment, unsafe conditions, injuries, accidents, exposure incidents and other hazards must be reported immediately. Volunteers must not attempt repairs or interventions outside their authority."
                        ),

                        CreateItem(
                            "3.4 Infection Prevention",
                            "Volunteers must follow applicable infection-control instructions and must not bring unnecessary personal items into restricted areas."
                        ),

                        CreateItem(
                            "3.5 Emergency Procedures",
                            "Volunteers must follow emergency procedures and instructions from authorised staff. Volunteers must not place themselves or residents at additional risk by attempting tasks for which they have not been trained."
                        ),

                        CreateItem(
                            "3.6 Restricted Activities",
                            "Volunteers must not administer medication, perform clinical procedures, operate restricted equipment or provide medical advice unless expressly authorised and appropriately qualified."
                        ),

                        CreateItem(
                            "3.7 Following Safety Instructions",
                            "Safety instructions issued by authorised SwiftCare personnel must be followed at all times."
                        )
                    )
                },
                cancellationToken);


            // =========================================================
            // 5. PROFESSIONAL BOUNDARIES
            // =========================================================

            await SeedPolicyAsync(
                context,
                new VolunteerPolicy
                {
                    Title = "Professional Boundaries and Appropriate Relationships",
                    Version = "1.0",
                    IsRequired = true,
                    IsActive = true,
                    PublishedDate = now,
                    CreatedDate = now,
                    ModifiedDate = now,

                    Items = CreateItems(

                        CreateIntro(
                            "Volunteers often develop positive relationships with residents. These relationships must remain appropriate, respectful and consistent with the volunteer's role."
                        ),

                        CreateItem(
                            "4.1 Professional Boundaries",
                            "Volunteers must maintain appropriate boundaries and must not use their position to obtain personal, financial, romantic or other inappropriate benefits."
                        ),

                        CreateItem(
                            "4.2 Money and Gifts",
                            "Volunteers must not request or accept money, loans, expensive gifts or personal financial benefits from residents or their families. Any offered gift that could create a conflict or appearance of favouritism must be discussed with a supervisor."
                        ),

                        CreateItem(
                            "4.3 Private Relationships",
                            "Volunteers must not establish inappropriate private relationships with residents or use personal communication channels to bypass SwiftCare procedures."
                        ),

                        CreateItem(
                            "4.4 Emotional Dependency",
                            "Volunteers must avoid creating unnecessary emotional or practical dependency. Volunteers should encourage residents to use appropriate SwiftCare support and staff resources."
                        ),

                        CreateItem(
                            "4.5 Personal Beliefs",
                            "Volunteers must respect the beliefs, culture, identity and personal choices of residents and must never pressure residents to adopt the volunteer's religious, political or personal beliefs."
                        ),

                        CreateItem(
                            "4.6 Physical Contact",
                            "Any physical contact must be appropriate and welcomed. Volunteers must not engage in sexual, romantic, threatening or otherwise inappropriate conduct."
                        ),

                        CreateItem(
                            "4.7 Boundary Concerns",
                            "Volunteers should discuss boundary concerns with their supervisor before a situation escalates."
                        ),

                        CreateItem(
                            "4.8 When in Doubt",
                            "When uncertain whether conduct is appropriate, volunteers must seek guidance from an authorised SwiftCare employee."
                        )
                    )
                },
                cancellationToken);


            // =========================================================
            // 6. ATTENDANCE, RESPONSIBILITIES AND COMMUNICATION
            // =========================================================

            await SeedPolicyAsync(
                context,
                new VolunteerPolicy
                {
                    Title = "Volunteer Attendance, Responsibilities and Communication",
                    Version = "1.0",
                    IsRequired = true,
                    IsActive = true,
                    PublishedDate = now,
                    CreatedDate = now,
                    ModifiedDate = now,

                    Items = CreateItems(

                        CreateIntro(
                            "Volunteers are expected to honour the days, times and responsibilities agreed with SwiftCare. Reliable attendance is important because residents and staff may depend on volunteer support."
                        ),

                        CreateItem(
                            "5.1 Attendance",
                            "Volunteers should arrive on time, follow sign-in procedures where applicable and report to their designated supervisor or responsible employee."
                        ),

                        CreateItem(
                            "5.2 Absence",
                            "If a volunteer cannot attend an agreed shift or activity, they must notify the appropriate SwiftCare contact as early as reasonably possible."
                        ),

                        CreateItem(
                            "5.3 Assigned Responsibilities",
                            "Volunteers must complete assigned activities responsibly and must ask for clarification when they do not understand an instruction. Volunteers must not accept responsibilities outside their approved role without authorisation."
                        ),

                        CreateItem(
                            "5.4 Communication",
                            "Communication with SwiftCare staff must remain respectful and professional. Concerns, difficulties, incidents and changes in availability should be communicated promptly."
                        ),

                        CreateItem(
                            "5.5 Dress and Identification",
                            "Volunteers must comply with applicable SwiftCare dress, identification and appearance requirements for their assigned activities."
                        ),

                        CreateItem(
                            "5.6 Personal Activities",
                            "Volunteers must not use volunteer time, SwiftCare facilities or resident interactions for unauthorised personal business."
                        ),

                        CreateItem(
                            "5.7 Supervisor Instructions",
                            "Volunteers must follow the direction of their designated supervisor and should report unresolved concerns through the appropriate SwiftCare management structure."
                        ),

                        CreateItem(
                            "5.8 Handover",
                            "Volunteers should complete required handover or reporting procedures before leaving."
                        ),

                        CreateItem(
                            "5.9 Attendance Review",
                            "Repeated unexplained absence, failure to communicate or refusal to perform reasonable authorised responsibilities may result in review of the volunteer placement."
                        )
                    )
                },
                cancellationToken);


            // =========================================================
            // 7. SOCIAL MEDIA, PHOTOGRAPHY AND PUBLIC COMMUNICATION
            // =========================================================

            await SeedPolicyAsync(
                context,
                new VolunteerPolicy
                {
                    Title = "Social Media, Photography and Public Communications",
                    Version = "1.0",
                    IsRequired = true,
                    IsActive = true,
                    PublishedDate = now,
                    CreatedDate = now,
                    ModifiedDate = now,

                    Items = CreateItems(

                        CreateIntro(
                            "Volunteers must protect resident privacy and the reputation of SwiftCare when using social media, messaging services or other public communication channels."
                        ),

                        CreateItem(
                            "6.1 Photography and Recording",
                            "Volunteers must not photograph, video or audio-record residents, employees, visitors, documents, care activities or restricted areas unless expressly authorised under an approved SwiftCare process."
                        ),

                        CreateItem(
                            "6.2 Confidential Information",
                            "Volunteers must not publish confidential information or identifying details about residents, families, staff or SwiftCare operations. This includes names, photographs, medical information, locations, conversations and personal stories."
                        ),

                        CreateItem(
                            "6.3 Consent and Publication",
                            "A resident's willingness to appear in a photograph or social-media post does not automatically authorise publication. Volunteers must follow SwiftCare's approved consent and communication procedures."
                        ),

                        CreateItem(
                            "6.4 Speaking on Behalf of SwiftCare",
                            "Volunteers must not represent themselves as an official spokesperson for SwiftCare or make statements on behalf of SwiftCare unless specifically authorised."
                        ),

                        CreateItem(
                            "6.5 Social Media Conduct",
                            "Volunteers must not use social media to harass, threaten, ridicule or disclose confidential information about residents, employees or other volunteers."
                        ),

                        CreateItem(
                            "6.6 Accidental Disclosure",
                            "If confidential information is accidentally posted, sent or disclosed, the volunteer must report the incident immediately and must not attempt to conceal it."
                        ),

                        CreateItem(
                            "6.7 Continuing Obligations",
                            "These requirements continue to apply after a volunteer's placement ends where the information concerned remains confidential."
                        ),

                        CreateItem(
                            "6.8 When in Doubt",
                            "When in doubt, volunteers must obtain permission from an authorised SwiftCare representative before publishing or sharing information related to SwiftCare."
                        )
                    )
                },
                cancellationToken);


            // =========================================================
            // FINAL SAVE
            // =========================================================

            await context.SaveChangesAsync(cancellationToken);
        }


        // =============================================================
        // SEED / UPDATE POLICY
        // =============================================================

        private static async Task SeedPolicyAsync(
            SwiftCareDbContext context,
            VolunteerPolicy seedPolicy,
            CancellationToken cancellationToken)
        {
            var existingPolicy =
                await context.VolunteerPolicies
                    .Include(x => x.Items)
                    .FirstOrDefaultAsync(
                        x =>
                            x.Title == seedPolicy.Title &&
                            x.Version == seedPolicy.Version,
                        cancellationToken);


            // =========================================================
            // POLICY DOES NOT EXIST
            // =========================================================

            if (existingPolicy == null)
            {
                context.VolunteerPolicies.Add(seedPolicy);

                return;
            }


            // =========================================================
            // POLICY ALREADY EXISTS
            // UPDATE ITS DETAILS
            // =========================================================

            existingPolicy.IsRequired =
                seedPolicy.IsRequired;

            existingPolicy.IsActive =
                seedPolicy.IsActive;

            existingPolicy.PublishedDate =
                seedPolicy.PublishedDate;

            existingPolicy.ModifiedDate =
                DateTime.Now;


            // =========================================================
            // REMOVE OLD ITEMS
            // =========================================================

            if (existingPolicy.Items != null &&
                existingPolicy.Items.Any())
            {
                context.VolunteerPolicyItems.RemoveRange(
                    existingPolicy.Items);
            }


            // =========================================================
            // CREATE NEW STRUCTURED ITEMS
            // =========================================================

            var newItems =
                seedPolicy.Items
                    .OrderBy(x => x.DisplayOrder)
                    .Select(
                        (item, index) =>
                            new VolunteerPolicyItem
                            {
                                VolunteerPolicyId =
                                    existingPolicy.VolunteerPolicyId,

                                Heading =
                                    item.Heading,

                                Description =
                                    item.Description,

                                IsSubheading =
                                    item.IsSubheading,

                                DisplayOrder =
                                    index + 1,

                                IsActive =
                                    item.IsActive,

                                CreatedDate =
                                    DateTime.Now,

                                ModifiedDate =
                                    DateTime.Now
                            })
                    .ToList();


            await context.VolunteerPolicyItems.AddRangeAsync(
                newItems,
                cancellationToken);
        }


        // =============================================================
        // INTRODUCTORY ITEM
        // =============================================================

        private static VolunteerPolicyItem CreateIntro(
            string description)
        {
            return new VolunteerPolicyItem
            {
                Heading = null,
                Description = description.Trim(),
                IsSubheading = false,
                IsActive = true
            };
        }


        // =============================================================
        // SUBHEADING ITEM
        // =============================================================

        private static VolunteerPolicyItem CreateItem(
            string heading,
            string description)
        {
            return new VolunteerPolicyItem
            {
                Heading = heading.Trim(),
                Description = description.Trim(),
                IsSubheading = true,
                IsActive = true
            };
        }


        // =============================================================
        // CREATE ORDERED ITEMS
        // =============================================================

        private static List<VolunteerPolicyItem> CreateItems(
            params VolunteerPolicyItem[] items)
        {
            var now = DateTime.Now;

            for (var i = 0; i < items.Length; i++)
            {
                items[i].DisplayOrder =
                    i + 1;

                items[i].CreatedDate =
                    now;

                items[i].ModifiedDate =
                    now;
            }

            return items.ToList();
        }
    }
}