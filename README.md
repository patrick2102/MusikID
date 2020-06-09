# DR-Music-recognition
The foundation of this project is Yvo Nelemans __Audiofingerprinting__ project: https://github.com/nelemans1971/AudioFingerprinting. Thank you also for our helpful email conversations.

# Github Workflow
 ## Create Issue
Create an issue for every change needed. 

**Remember to add the project to the issue! Github is not directly coupled to the project board!!**
 When an issue is created with a title and some more descriptive text and or pictures, it will automatically be added on "to do" on the project board. 
  ## Create Branch
 When working on an issue create a branch using the github interface
 
 or with the git command. **Remember** to checkout to develop branch and pull first!
 ```
 git checkout develop
 git pull origin develop
 git branch -b branch_name 
 ```
 
 To have the issue move automatically to "in progress" on the project board, there have to exist a pull request for the issue.
 
 This means that you either have to move it manually to "in progress", or push the first commit you do to the origin repo and create a pull request with no reviewer. (might result in a lot of ongoing pull requests, making it hard to find the finished onces).
 
 ## Create Pull Request
 When creating a pull request:
 Add a reviewer to let the code be reviwed before being merged. 


 **Remember** to add a label of either app or web!!

 **Remember** to add a reference to the issue with either one of these keywords:
* close
* closes
* closed
* fix
* fixes
* fixed
* resolve
* resolves
* resolved

 with a #issue_id after it, e.g:
```
resolves #1.
```
The reference to the issue both closes the issue, and moves it on the project board to "done", when merged into develop.


 ## Review Code
**Nothing is allowed to be merged onto develop unless it has been reviewed!**

 We're working on many different things and need to make sure that nothing that is merged, unintentionally breaks something, is confusing or could easily be made better.

 Read the code of what is about to be changed, and add comments to anything confusing, something that breaks conventions or actually breaks something, e.g. Someone could accidentially add something to the branch that should not be there/is unrelated to the issue (happens quite often).

 No one can have the full overview of the code base when we're 6 people working on it, and some of the parts of the projects are not related at all,

 ## Merge Pull Request
**Double check that the issue is actually referenced!**

 **Make sure your branch is completely up-to-date with develop before merging!**

 **Fix any potential conflict inside the branch first!**

 Only merge after the pull request has been approved by all the reviewers.

 When the merge happens, then the pull request, branch and issue are closed along with the issue being moved to "done" on the project board. The branch is effectively deleted, but can be restored from the closed pull request if needed.
