---
module: community
title: Community
icon: bi-chat-square-text
order: 115
category: Knowledge
color: #0d9488
example: DEMO · Yellow leaves on paddy
updated: 2026-11-17
note: New guide
---

::: section overview
title: What this module does for you
kind: overview

The **SPIC Knowledge Community** is a place to ask questions about products and farming, share what you have seen in the field, and get answers from SPIC experts and other farmers.

- ask | icon: bi-patch-question | color: #0d9488
  title: Ask a question
  text: Write your problem in your own words. Add photos or documents if they help.
- answer | icon: bi-patch-check | color: #16a34a
  title: Get expert answers
  text: SPIC staff reply as **SPIC Expert**. Other farmers and dealers can reply too.
- learn | icon: bi-people | color: #2563eb
  title: Learn from others
  text: Search old discussions, follow the ones you like and join product groups.
:::

::: section flow
title: How a question moves, and who acts
kind: flow

Each row is a group of people and each box is a step. Your row is marked "You". Tap a step to see what happens there.

- members | type: lane | color: #0d9488 | roles: Farmer, Dealer
  title: Farmers and dealers
  sub: Ask, reply, share
- experts | type: lane | color: #16a34a | roles: Admin, CorporateAdmin, Director, AVP, SMD, SMM, RM, RMD, MDO, MO, JMDO, SpecialAdmin, SuperAdmin, MDAdmin, MDHOD
  title: SPIC experts
  sub: SPIC staff
- ask | lane: members | col: 1 | icon: bi-pencil-square | color: #d97706 | next: reply, expert | screen: ask-question
  title: Ask a question
  status: Waiting For Reply
  text: Fill the **Create a Discussion** form and press **Post Discussion**. The question waits for its first reply.
- similar | lane: members | col: 2 | icon: bi-search | color: #ea580c | kind: exception | from: ask | to: ask | screen: ask-question
  title: Similar question found
  text: The app shows discussions that look like yours. Open one to read the answer, press **Back to edit**, or press **Post Anyway**.
- reply | lane: members | col: 3 | icon: bi-chat-dots | color: #2563eb | next: resolve | screen: discussion-details
  title: A member replies
  status: Open
  text: Another farmer or dealer replies. A question that was waiting becomes **Open**.
- expert | lane: experts | col: 3 | icon: bi-patch-check | color: #16a34a | next: resolve | screen: discussion-details
  title: An expert replies
  status: Expert Answer
  text: When SPIC staff reply, the discussion shows **Expert Answer**.
- resolve | lane: members | col: 4 | icon: bi-check2-circle | color: #64748b | return: reply | screen: discussion-details
  title: Mark as resolved
  status: Resolved
  text: The person who asked presses **Mark as Resolved** when the problem is solved. **Reopen** makes it **Open** again.
:::

::: section statuses
title: Every status you can see
kind: statuses

- waiting | color: #d97706
  label: Waiting For Reply
  text: Posted, but nobody has replied yet.
- waiting-answers | color: #d97706
  label: Waiting for Answers
  text: Shown on the "posted" screen just after you ask. It means the same as Waiting For Reply.
- open | color: #2563eb
  label: Open
  text: Members have replied. The discussion is still going on.
- expert | color: #16a34a
  label: Expert Answer
  text: A SPIC expert has replied.
- resolved | color: #64748b
  label: Resolved
  text: The person who asked marked the problem as solved. It can be reopened.
:::

::: section roles
title: Who does what
kind: roles

Your card is highlighted. Everyone who signs in can read and join discussions.

- members | color: #0d9488 | roles: Farmer, Dealer
  title: Farmers and dealers
  - Ask questions and add photos or files
  - Reply, like, save and follow discussions
  - Edit, delete or resolve your own questions
- experts | color: #16a34a | roles: Admin, CorporateAdmin, Director, AVP, SMD, SMM, RM, RMD, MDO, MO, JMDO, SpecialAdmin, SuperAdmin, MDAdmin, MDHOD
  title: SPIC experts (SPIC staff)
  - Answer questions. Your replies show **SPIC Expert**
  - Ask and share like any member
- moderators | color: #475569 | roles: Admin, CorporateAdmin
  title: Admin and Corporate Admin
  - Mark any discussion as resolved, or reopen it
  - Delete any reply
:::

::: section faq
title: Problems and answers
kind: faq

- cannot-post | popular: true
  q: I press Post Discussion and red messages appear
  a: The title needs at least 10 characters, a **Category** must be chosen, and the **Description** needs at least 20 characters. Fix the field in red and press **Post Discussion** again.
- similar | popular: true
  q: The app says "We found similar discussions". What do I do?
  a: Your question may already have an answer. Press **View Discussion** to read it. If your question is different, press **Post Anyway**. To change your text, press **Back to edit**.
- edit-delete
  q: How do I change or delete my question?
  a: Open **Discussions**, go to the **My Discussions** tab and tap the three dots on your question. Choose **Edit** or **Delete**.
- no-resolve
  q: I do not see Mark as Resolved
  a: Only the person who asked, or an Admin, can mark a discussion as resolved or reopen it.
- file-not-attached
  q: My file or photo was not attached
  a: Each file on a question must be 10 MB or smaller, and a question can carry up to 6 files. A reply can carry up to 3 images of 5 MB each.
- expert-answers
  q: How do I find answers from SPIC experts?
  a: Open **Discussions** and tap the **Expert Answers** tab. In a discussion, expert replies show the **SPIC Expert** tag.
:::

::: section community-home
title: Use the Knowledge Community home page
routes: /Community
minutes: 2
screenshot: community-home.png
screenshot-phone: community-home.phone.jpg

**What this screen is for**
The **SPIC Knowledge Community** home page is where you start. You can ask a question, see the newest discussions and open the discussions for a product.

1. Type your question or a product name in the search box and press **Ask a Question**. The question form opens with your words as the title.
2. Look at the numbers under the search box: **Community Members**, **Discussions** and **Expert Answer**.
3. Under **Recent Discussions**, tap a discussion to open it. Use the two small buttons to show the list as a grid or as rows.
4. Press **View All Discussions** to see the full list with tabs and filters.
5. Under **Popular Product**, press **Join** to join a product group. It changes to **Joined**. Press it again to leave.
6. Press **Discussions** on a product card to see only the discussions about that product.
7. In the **New Here?** box, press **Ask a Question** to start a new question.

> Tip: The app remembers whether you chose the grid or the row layout.

## Changing a product picture

- Admin and staff who can edit the Digital Library see a camera icon on each product picture.
- Tap the camera and choose a JPG, PNG or WEBP picture up to 3 MB.

## Common problems

- **"Discussions could not be loaded".** The connection failed. Press **Try again**.
- **"No discussions yet".** Nobody has asked yet. Press **Ask a Question** to be the first.
- **"That picture is larger than 3 MB."** Choose a smaller product picture.
:::

::: section browse-discussions
title: Find and follow discussions
routes: /Community/discussions
minutes: 3
tour: 1 > [data-help=cm-list-stats] | 2 > [data-help=cm-list-tabs] | 3 > [data-help=cm-list-filters] | 4 > [data-help=cm-list-tools] | 5 > [data-help=cm-list-card] | 6 > [data-help=cm-list-new]
screenshot: browse-discussions.png
hotspots: 1 @ 2.2,9.8,95.5,16.1 | 2 @ 2.2,28.3,95.5,5 | 3 @ 2.2,35.7,95.5,42.2 | 4 @ 52.8,82.3,43.4,4.8 | 5 @ 2.2,80.2,95.5,19.8 | 6 @ 82.4,1.9,15.4,4.8
screenshot-phone: browse-discussions.phone.jpg
hotspots-phone: 1 @ 3.1,9.2,93.8,17.5 | 2 @ 0,27.9,100,3.2 | 3 @ 3.1,32.2,93.8,3.5 | 4 @ 6.9,39.9,86.2,6.8 | 5 @ 3.1,36.8,93.8,63.2 | 6 @ 3.1,5.4,42.2,3

**What this screen is for**
The **Discussions** screen lists every question in the community. You can search, filter, follow discussions and manage your own questions.

1. Look at the tiles at the top: **Total Discussions**, **Active Members**, **Expert Answers** and **New This Month**.
2. Choose a tab: **All Discussion**, **My Discussions**, **Trending**, **Unanswered**, **Following** or **Expert Answers**.
3. Open **Filters** to choose a **Date Range**, **Category**, **Product**, **Crop**, **Status** or **Tags**, then press **Apply Filters**. **Clear All** removes them.
4. Use **Sort by:** (**Latest Activity**, **Most Replies**, **Most Views** or **Oldest**) and type in **Search discussions...** to find a topic.
5. Tap a discussion to open it. Press **Follow** under the person's picture to follow it. On your own questions, tap the three dots to **Edit** or **Delete**.
6. Press **Start a Discussion** to ask a new question.

> Careful: Deleting a discussion removes it and its replies from the community.

## What the tabs show

- **My Discussions**: questions you asked.
- **Trending**: the most active questions of the last 30 days.
- **Unanswered**: questions with no reply yet.
- **Following**: questions you follow.
- **Expert Answers**: questions a SPIC expert has answered.

## Common problems

- **"No discussions found".** Try another tab or press **Clear filters**.
- **There are no three dots on a discussion.** You can edit or delete only your own questions.
- **Tags filter does nothing.** Type a tag and press Enter before you press **Apply Filters**.
:::

::: section ask-question
title: Ask a question
routes: /Community/new, /Community/posted/{id}
minutes: 3
tour: 1 > [data-help=cm-new-title] | 2 > [data-help=cm-new-choices] | 3 > [data-help=cm-new-tags] | 4 > [data-help=cm-new-desc] | 5 > [data-help=cm-new-files] | 6 > [data-help=cm-new-post]
screenshot: ask-question.png
hotspots: 1 @ 4.4,17.1,91.2,8.8 | 2 @ 4.4,28.1,91.2,10.8 | 3 @ 4.4,38.9,91.2,11.5 | 4 @ 4.4,52.6,91.2,23.7 | 5 @ 2.2,81.9,95.5,18.1 | 6 @ -
screenshot-phone: ask-question.phone.jpg
hotspots-phone: 1 @ 7.4,11,85.1,5.5 | 2 @ 7.4,17.9,85.1,20.5 | 3 @ 7.4,38.4,85.1,7.3 | 4 @ 7.4,47,85.1,14.9 | 5 @ 3.1,65.2,93.8,22.7 | 6 @ 43.8,91.4,52.1,3.3

**What this screen is for**
Use the **Create a Discussion** screen to ask the community a question or share your experience. SPIC experts and other farmers can then reply.

1. Type your question in **Discussion Title**.
2. Choose a **Category**. If it is about a product or crop, choose the **Related Product** and the **Crop** too.
3. Under **Tags**, type a short word, such as a disease or problem, and press Enter or comma. You can add up to 6 tags.
4. In **Description**, write your question, what you tried and what you saw.
5. Under **Attachments**, drop photos or files in the box or press **Browse Files**. Tap the cross to remove a file.
6. Press **Post Discussion**. If the app finds **We found similar discussions**, read them or press **Post Anyway**. If it says **Your question looks unique!**, press **Post Discussion** again.

> Tip: A clear photo of the leaf, plant or product label helps the experts answer faster.

## After you post

- The **Your question has been posted** screen shows the title, **Category**, **Posted** time, **Status** and **Views**.
- Press **View Discussion** to open your question, or **Back to Community** to go to the home page.

## Edit your question

- On **Discussions**, tap the three dots on your question and choose **Edit**. The same form opens.
- Change the text and press **Save Changes**.
- **Cancel** asks before it throws away what you typed. Press **Keep editing** to stay.

## Fields explained

| Field | What to enter |
|---|---|
| **Discussion Title** | Your question in one line. At least 10 characters. Required. |
| **Category** | The type of question. Required. |
| **Related Product** | The SPIC product it is about, if any. |
| **Crop** | The crop, for example paddy or tomato. |
| **Tags** | Up to 6 short words that help others find it. |
| **Description** | The full question. At least 20 characters. Required. |
| **Attachments** | Images, PDF or Word files, up to 10 MB each. |

## Common problems

- **"Please enter a discussion title."** or **"The title needs at least 10 characters."** Write a longer title.
- **"Please add a little more detail (at least 20 characters)."** Write more in **Description**.
- **"That file is larger than 10 MB and was not attached."** Make the file smaller or take a new photo.
- **"Your question was posted, but the attachments could not be uploaded."** The question is saved. Check your connection.
:::

::: section discussion-details
title: Read a discussion and reply
routes: /Community/discussion/{id}
minutes: 3

**What this screen is for**
The **Discussion Details** screen shows one question with its photos and files, and all the replies. Here you reply, like, save, follow and share.

1. At the top, see who asked, their tag (**Farmer**, **Dealer** or **SPIC Expert**), the date and the status.
2. Read the question. Tap a photo to see it large. Tap a file to open it.
3. Use **Save**, **Share** and **Follow** at the top, and the thumb button to like the question.
4. Under **Replied**, read the replies. Use **Sort by:** to choose **Latest**, **Oldest** or **Most Liked**.
5. To answer one reply, tap its **reply** button. A box opens with the person's name. Write your answer and press **Reply**.
6. To answer the question, type in **Write a reply...** at the bottom. Add up to 3 images or a link if needed, then press the send button.
7. When your problem is solved, press **Mark as Resolved** and confirm. Press **Reopen** if you need more help.

> Tip: On a computer, Enter sends the reply. Shift and Enter start a new line.
> Careful: The bin icon on a reply deletes it. You can delete only your own replies. An Admin can delete any reply.

## Common problems

- **"Discussion not found".** The question was deleted. Press **Back to Discussions**.
- **"Please write a few words with your images."** A reply needs some text, not only images.
- **"A reply can carry up to 3 images."** Remove an image or post the rest in another reply.
- **No Mark as Resolved button.** Only the person who asked, or an Admin, can resolve or reopen.
:::
