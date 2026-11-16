---
module: digital-library
title: Digital library
icon: bi-book
order: 114
category: Knowledge
color: #0e7490
example: DEMO Product · Brochure PDF
updated: 2026-11-17
note: New guide
---

::: section overview
title: What this module does for you
kind: overview

The Digital Library keeps SPIC product information, AI videos and product brochures in one place. Anyone can search it, read it and ask SPIC AI questions about it. A small team adds and publishes the content.

- find | icon: bi-search | color: #0e7490
  title: Find what you need
  text: Browse AI videos, product information and brochures, or search by name.
- ask | icon: bi-stars | color: #7c3aed
  title: Ask SPIC AI
  text: Type a question and get an answer from the library content.
- manage | icon: bi-pencil-square | color: #d97706
  title: The library team adds content
  text: They add items, publish them and hide old ones.
:::

::: section flow
title: How content reaches you, and who acts
kind: flow

Each row is a role and each box is a step. Your row is marked "You". Tap a step to see what happens there.

- team | type: lane | color: #d97706 | roles: Admin
  title: Library team
  sub: Staff who manage the library
- readers | type: lane | color: #0e7490 | roles: CorporateAdmin, Director, AVP, SMD, SMM, RM, RMD, MDO, MO, JMDO, MDAdmin, MDHOD, Dealer, Farmer
  title: Everyone
  sub: All signed-in users
- create | lane: team | col: 1 | icon: bi-pencil-square | color: #64748b | next: publish | screen: add-content
  title: Add the content
  status: Draft
  text: Fill the three steps of the add form. **Save as Draft** keeps it hidden from readers.
- publish | lane: team | col: 2 | icon: bi-broadcast | color: #16a34a | next: read | return: create | screen: manage-content
  title: Publish
  status: Published
  text: Press **Save & Publish** or **Publish**. **Unpublish** moves it back to Drafts.
- read | lane: readers | col: 3 | icon: bi-book | color: #0e7490 | next: ask | screen: library-home
  title: Find and read
  status: Published
  text: Everyone can open published videos, product pages and brochures.
- ask | lane: readers | col: 4 | icon: bi-stars | color: #7c3aed | screen: ask-ai
  title: Ask SPIC AI
  text: Ask a question in the chat. The answer lists the library items it used.
- archive | lane: team | col: 3 | icon: bi-archive | color: #475569 | kind: exception | from: publish | to: create | screen: manage-content
  title: Hide old content
  status: Archived
  text: Old or wrong content is archived and readers no longer see it. **Restore** brings it back as a draft.
:::

::: section statuses
title: Every status you can see
kind: statuses

- published | color: #16a34a
  label: Published
  text: Everyone can see and open it.
- draft | color: #d97706
  label: Draft
  text: Saved but hidden from readers. Only the library team sees it.
- archived | color: #475569
  label: Archived
  text: Hidden from readers. The library team can restore it any time.
- active | color: #0e7490
  label: Active
  text: A category that is offered on the add form.
- inactive | color: #64748b
  label: Inactive
  text: A category that is no longer offered on the add form.
:::

::: section roles
title: Who does what
kind: roles

Your card is highlighted. You only see the screens your role uses.

- readers | color: #0e7490 | roles: CorporateAdmin, Director, AVP, SMD, SMM, RM, RMD, MDO, MO, JMDO, MDAdmin, MDHOD, Dealer, Farmer
  title: Everyone
  - Browse and search the library
  - Watch videos and open brochures
  - Ask SPIC AI questions
- team | color: #d97706 | roles: Admin
  title: Library team
  - Add products, AI videos and brochures
  - Publish, unpublish, archive and delete content
  - Keep the categories list up to date
:::

::: section faq
title: Problems and answers
kind: faq

- open-brochure | popular: true
  q: How do I read a product brochure?
  a: Open **Product Brochures**, tap the brochure, then press **Open brochure** under the cover picture. The PDF opens or is saved to your phone.
- ai-answer | popular: true
  q: Can I trust every answer from SPIC AI?
  a: SPIC AI answers from the library content, but it can make mistakes. Open the items listed under the answer and check the facts before you advise a farmer or dealer.
- no-add
  q: I do not see the Add Content button
  a: Only the library team can add content. Your designation needs permission to add Digital Library content. Ask your administrator if you need it.
- only-published
  q: I see "Only published Digital Library content is available to you"
  a: You can only see published content. In the status list choose **All Status** or **Published**, or tap one of the first three cards.
- category-in-use
  q: I cannot delete a category
  a: Library items still use it. Turn it off with the toggle instead. It is then no longer offered on the add form.
:::

::: section library-home
title: Find content in the library
routes: /DigitalLibrary
minutes: 2
tour: 1 > [data-help=dl-actions] | 2 > [data-help=dl-search] | 3 > [data-help=dl-categories] | 4 > [data-help=dl-featured]
screenshot: library-home.png
hotspots: 1 @ 5.2,22.1,89.5,5.3 | 2 @ 14.5,30.2,71,6.2 | 3 @ 5.2,51.1,89.5,23.8 | 4 @ 2.2,78.7,95.5,21.3
screenshot-phone: library-home.phone.jpg
hotspots-phone: 1 @ 7.2,11.1,85.6,3 | 2 @ 7.2,15.8,85.6,3.9 | 3 @ 7.2,28.3,85.6,39 | 4 @ 3.1,68.6,93.8,31.4

**What this screen is for**
The **Digital Library Assistant** screen is the start page of the library. From here you can ask SPIC AI, open a type of content, or open a featured item.

1. Press **Start Chat** to ask SPIC AI a question. If you are in the library team, you also see **Add Content** here.
2. Type a product name or a question in the search box and press the arrow. You can also tap a suggestion, such as **Find Product Information** or **Show latest brochures**. This opens the SPIC AI chat with your question.
3. Under **Digital Library**, tap **AI Videos**, **Product Information** or **Product Brochures**. Each card shows how many items it has.
4. Under **Featured Content**, tap **Open** (or **Explore Videos**) on an item. Press **View All Content** to see the full list.

> Tip: The search box here asks SPIC AI. To find an item by its name, press **View All Content** and use its search box.

## Common problems

- **"Nothing in the library yet".** No content is published yet. Check again later.
- **"The library is not available".** The connection failed. Press **Try again**.
- **My search opened a chat.** This is normal. The search box on this page asks SPIC AI.
:::

::: section all-content
title: Browse and search all content
routes: /DigitalLibrary/content
minutes: 2
screenshot: all-content.png
screenshot-phone: all-content.phone.jpg

**What this screen is for**
The **All Content** screen lists every item in the library as cards. The line under the title shows how many items match.

1. At the top, tap a card, such as **Product Information**, **AI Videos** or **Product Brochures**, to show only that type. The number is how many items of that type there are.
2. You can also use the tabs **All Types**, **AI Videos**, **Products** and **Brochures**.
3. Type in the **Search content...** box. It finds words in the title, description, keywords, tags and category.
4. Tap the picture or the title of a card to open the item.
5. Press **Load More** at the bottom to see more items.

> Tip: The back arrow at the top returns you to the library start page.

## Common problems

- **"No content found".** Your type or search is too narrow. Press **Clear filters**.
- **"Only published Digital Library content is available to you".** Choose **All Status** or **Published** in the status list.
- **"The library is not available".** The connection failed. Press **Try again**.
:::

::: section view-item
title: Read a product, video or brochure
routes: /DigitalLibrary/view/{Id:int}
minutes: 2

**What this screen is for**
This screen shows one library item: a product page, an AI video or a brochure. The library team chooses which parts are shown and in what order, so some parts may be missing.

1. Open an item from the library start page, from **All Content** or from a related item.
2. At the top, see the type (**Product**, **AI Video** or **Brochure**), the title, a short description and the tags. Press **Back** to return to the list.
3. In the overview part, play the video, see the product picture, or press **Open brochure** to open the PDF.
4. In the **SPIC AI Chatbox**, press **Start Conversation** to ask SPIC AI about this item.
5. In the **Details** box, see the **Category**, **Published** date, **Author**, **Status**, **Views** and tags.
6. Scroll down for related videos and brochures, key features and usage. Tap a related item to open it.

## Common problems

- **"Content not found".** The item was deleted or is not published now. Press **Back to All Content**.
- **"No video has been uploaded or linked yet."** The library team has not added the video yet.
- **"No brochure file has been uploaded yet."** The library team has not added the PDF yet.
- **The video opens outside the app.** Some video links can only open in their own website. Press **Open the video**.
:::

::: section ask-ai
title: Ask SPIC AI a question
routes: /DigitalLibrary/chat, /DigitalLibrary/chat/{ConversationId:int}
minutes: 2
tour: 1 > [data-help=dl-chat-new] | 2 > [data-help=dl-chat-body] | 3 > [data-help=dl-chat-input] | 4 > [data-help=dl-chat-tools] | 5 > [data-help=dl-chat-list] | 6 > [data-help=dl-chat-share]
screenshot: ask-ai.png
hotspots: 1 @ 3.8,10.2,23.1,5.3 | 2 @ 28.5,10.3,69.2,68.1 | 3 @ 32.1,81.4,61.9,4.8 | 4 @ 32.1,87.4,61.9,4.8 | 5 @ 2.3,2,26.2,96.3 | 6 @ 74,3.7,8.8,4.8
screenshot-phone: ask-ai.phone.jpg
hotspots-phone: 1 @ - | 2 @ 0,5.7,100,71.4 | 3 @ 5.9,78.6,88.2,3 | 4 @ 5.9,82.3,88.2,7.3 | 5 @ - | 6 @ 71.3,1.6,11.8,3.3

**What this screen is for**
On the **Digital Library Assistant** chat screen you ask SPIC AI about SPIC products, videos and brochures. Answers come from the library content and list the items they used.

1. Press **New Chat** to start a new conversation.
2. Tap a suggestion card, such as **Tell me about a SPIC product** or **Recommend videos**. The answers appear here. Tap a library item under an answer to open it, or tap a follow-up question.
3. Or type your own question in the box **Ask anything about SPIC products, videos, brochures**.
4. Choose **Select Source** to search only **AI Videos**, **Product Information** or **Product Brochures**. Press **Attach** to ask about one library item, or **Voice** to speak your question. Then press **Send**.
5. Your past chats are listed on the left under **Today**, **Yesterday** and **Earlier**. Tap one to open it. The bin deletes it.
6. Press **Share** to send the conversation to someone, or copy it.

> Tip: After **Attach**, the box shows **Asking about:** with the item name. Every question is answered from that item until you press the cross.
> Careful: SPIC AI may display inaccurate info. Check the answer before you act on it.

## Common problems

- **"SPIC AI could not answer just now."** Wait a moment and send the question again.
- **"Voice input is not available on this device."** Type your question instead.
- **"Start a conversation to share it."** Ask a question first, then press **Share**.
- **"Answers from library content" under an answer.** The answer was put together by matching words in the library items, without the full AI.
:::

::: section add-content
title: Add or edit library content
routes: /DigitalLibrary/add/{KindRoute}
minutes: 5

**What this screen is for**
The library team uses this form to add a product page, an AI video or a brochure, or to change one. The form has three steps: **Content & Details**, **Related Content** and **Page Layout**.

1. On the library start page or on **All Content**, press **Add Content**. In **Create New Content**, choose **AI Videos**, **Product Information** or **Product Brochures**. To change an item, press its pencil icon or **Edit Content** instead.
2. In **Basic Information**, choose the category and fill the title, tags and **Short Description**. For a product, also choose **Associated Program / Sub-Category**, **Resource Content Format** and **Visibility / Enable To**, then fill **Product Title** and **Keywords (Comma Separated)**.
3. Add the files: the product image, the video file or **Video URL** with a thumbnail, or the brochure cover and the **Brochure PDF**.
4. For a product or video, fill the overview, **Key Features / Benefits** and **Usage / Application**. Press **Add Section** to add more text with its own title.
5. Press **Next: Related Content**. Tick the published videos and brochures to show on the item page. Use the arrows to change their order and **Max Item** to limit how many are shown.
6. Press **Next: Page Layout**. Drag the parts, or use the arrows, to set their order. Use the switch to hide a part and the pencil to rename it.
7. Press **Save & Publish** (or **Save Changes** when editing). **Save as Draft** saves it without showing it to readers.

> Tip: If a category is missing, choose **+ Add new category…** in the list. **Manage categories** opens the full categories list.
> Careful: Files are uploaded after the item is saved. Stay on the page until the upload messages finish.

## Fields explained

| Field | What to enter |
|---|---|
| **Product Title**, **Video Title** or **Brochure Title** | The name readers will see. Required before you can go to the next step. |
| **Tags** | Short words to help search. Type one and press Enter, or tap a tag under **Used before:**. |
| **Product Image** or **Video Thumbnail** | PNG or JPG, up to 5 MB. Best size 1280 x 720 px. |
| **Video File (Upload)** | MP4, WebM or MOV, up to 700 MB. Or give a YouTube, Vimeo or direct **Video URL**. |
| **Duration** | Optional, as hh:mm:ss. Shown on the item page. |
| **Brochure PDF** | One PDF file, up to 25 MB. |

## Common problems

- **"Enter a product title before continuing."** Fill the title in step 1 first.
- **"Image must be 5 MB or smaller."** Use a smaller picture. The same rule is 700 MB for a video and 25 MB for a brochure.
- **"could not be uploaded. The item itself was saved."** Open the item again with the pencil icon and choose the file again.
- **"No published videos found" in step 2.** Only published items can be linked. Publish them first.
- **"You do not have permission to add content".** Only the library team can add or edit content.
:::

::: section manage-content
title: Publish, hide or delete content
routes: /DigitalLibrary/content, /DigitalLibrary/view/{Id:int}
for: DigitalLibrary.Update
minutes: 3
screenshot: manage-content.png
screenshot-phone: manage-content.phone.jpg

**What this screen is for**
The library team decides what readers can see. Each card shows a tag: **Published**, **Draft** or **Archived**. The buttons below appear only for staff with permission to change library content.

1. On **All Content**, tap **Draft Content** or **Archived** to see hidden items, or choose **Published**, **Draft** or **Archived** in the **All Status** list.
2. Press **Publish** on a draft card to show it to everyone.
3. Press **Unpublish** to hide a published item. In **Unpublish this content?**, press **Unpublish**. It moves to Drafts.
4. Press the archive icon to archive an old item. Press **Restore** to bring an archived item back as a draft.
5. Press the pencil icon to change an item, or the bin icon to delete it, then press **Delete** to confirm.
6. On an item page, the **Details** box also has **Edit Details** and a publish button, such as **Publish Product** or **Unpublish Video**.

> Careful: Deleting content cannot be undone. Archive it instead if you may need it again.

## Common problems

- **I do not see the bin icon.** Deleting needs a separate permission. Ask your administrator.
- **An item disappeared from the list.** It may now be archived or a draft. Choose **All Status** in the status list.
:::

::: section categories
title: Manage library categories
routes: /DigitalLibrary/categories
minutes: 3
screenshot: categories.png
screenshot-phone: categories.phone.jpg

**What this screen is for**
The **Library Categories** screen keeps the list of categories and sub-categories offered on the add form, for example a category with its sub-categories under it.

1. Type in the search box, or choose **All**, **Active** or **Inactive**, to find a category.
2. Tap the arrow next to a category to show its sub-categories.
3. Press **Add Category**, type the **Name** and an optional **Sort order**, then press **Save**.
4. Press **Add Sub-Category** (or **Add sub-category to** under a category), choose the **Category**, type the **Name** and press **Save**.
5. Press the pencil icon to change a name or order, then press **Save**. A new name is also changed on the items that use it.
6. Use the toggle to make a category **Inactive** or **Active** again. Inactive ones are not offered on the add form.
7. Press the bin icon and **Delete** to remove a category that no item uses.

## Common problems

- **"You do not have permission to manage categories".** Only the library team can change categories. Press **Back to the library**.
- **"is in use by ... so it cannot be deleted. Deactivate it instead."** Turn it off with the toggle.
- **"has ... sub-categories. Delete or move them first".** Delete or move its sub-categories, or turn the category off.
- **Add Sub-Category is grey.** Add a category first.
- **"Sort order must be a whole number of 0 or more."** Leave it empty or type 0, 1, 2 and so on.
:::

::: section coming-soon
title: A library page that is not ready yet
routes: /DigitalLibrary/{Section}
minutes: 1

**What this screen is for**
Some Digital Library addresses open a page that says the section **is being built**. That part of the library is not ready yet.

1. Read the message. It says the section will be available in an upcoming release.
2. Press **Back to the library** to return to the start page.

## Common problems

- **I reached this page from a saved link.** Open the library from the menu and use **View All Content** instead.
:::
